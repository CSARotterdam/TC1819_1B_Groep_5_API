using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace API {
	class Config {
		//Default JSON string
		private static readonly string defaultJson =
@"{
	//Settings for the server database.
	""databaseSettings"": {
		//The database server to connect to.
		""serverAddress"": null,
		""database"": null,
		//Login details for the above server.
		""username"": null,
		""password"": null,		
		//The timeout for database queries, in seconds. -1 to disable.
		""connectionTimeout"":-1,
		//Whether to keep authenticating the server with the database server.
		""persistLogin"": true,
		//Cache the results of database queries, which can lower response time.
		""caching"": true,
	},

	//Connection connections
	""connectionSettings"": {
		//If true, the server will attempt to automatically detect its IP address by pinging 8.8.8.8
		""autodetect"": true,
		//A list containing the IP addresses the server will bind to. If autodetect is enabled,
		//the detected address will be added to this list.
		""serverAddresses"": [
			""localhost""
		]
	},

	//Advanced performance settings
	""performanceSettings"":{
		//The amount of worker threads that the server will use. More threads increases performance, but also increases hardware usage.
		""workerThreadCount"": 5,
	},

	//User authentication settings
    ""authenticationSettings"":{
		//The amount of time (in seconds) until user tokens expire, forcing clients to authenticate themselves again.
        ""expiration"": 7200,
		
		//Filters that specify the username requirements. Usernames must match one of the specified filters.
		//If empty, all usernames will be allowed.
		""usernameRequirements"": [
			{
				""regex"": ""([A - z])"",
				""length"": 5
			},
			{
				""regex"": ""([0 - 9])"",
				""length"": 7
			}
	
		]
    },

	//Request settings
	""requestSettings"":{
		//The max duration that clients can reserve items for.
		""maxLoanDuration"": ""21:00:00:00""
	}
}";
		//End of default JSON string

		public static JObject loadConfig() {
			string filename = "config.json";
			Logger log = Program.log;
			JObject settings;


			if (!File.Exists(filename)) {
				settings = JObject.Parse(defaultJson);
				using (StreamWriter writer = File.CreateText(filename)) {
					writer.Write(defaultJson);
				}
			} else {
				try {
					settings = JObject.Parse(File.ReadAllText(filename));
				} catch (JsonReaderException) {
					log.Fatal("The configuration file could not be read.");
					log.Fatal("Ensure that it is a valid configuration file");
					return null;
				}
			}

			//Check database configuration
			JObject DBSettings = settings["databaseSettings"].ToObject<JObject>();
			bool databaseSuccess = true;

			DBSettings.TryGetValue("username", out JToken username);
			if (username == null || username.Type != JTokenType.String) {
				log.Error("Database username setting not set.");
				databaseSuccess = false;
			}
			DBSettings.TryGetValue("password", out JToken password);
			if (password == null || password.Type != JTokenType.String) {
				log.Error("Database password setting not set.");
				databaseSuccess = false;
			}
			DBSettings.TryGetValue("serverAddress", out JToken serverAddress);
			if (serverAddress == null || serverAddress.Type != JTokenType.String) {
				log.Error("Database server address setting not set.");
				databaseSuccess = false;
			}
			DBSettings.TryGetValue("database", out JToken database);
			if (database == null || database.Type != JTokenType.String) {
				log.Error("Database setting not set.");
				databaseSuccess = false;
			}
			DBSettings.TryGetValue("connectionTimeout", out JToken connectionTimeout);
			if (connectionTimeout == null || connectionTimeout.Type != JTokenType.Integer) {
				log.Error("Connection timeout setting not set.");
				databaseSuccess = false;
			}
			DBSettings.TryGetValue("persistLogin", out JToken persistLogin);
			if (persistLogin == null || persistLogin.Type != JTokenType.Boolean) {
				log.Error("PersistLogin setting not set.");
				databaseSuccess = false;
			}
			DBSettings.TryGetValue("caching", out JToken caching);
			if (caching == null || caching.Type != JTokenType.Boolean) {
				log.Error("Caching setting not set.");
				databaseSuccess = false;
			}

			//Check connection settings
			JObject ConSettings = settings["connectionSettings"].ToObject<JObject>();
			bool connectionSuccess = true;

			ConSettings.TryGetValue("autodetect", out JToken autodetect);
			if (autodetect == null || autodetect.Type != JTokenType.Boolean) {
				log.Error("Autodetect setting not set.");
				connectionSuccess = false;
			}

			ConSettings.TryGetValue("serverAddresses", out JToken serverAddresses);
			if (serverAddresses == null || serverAddresses.Type != JTokenType.Array) {
				log.Error("Server address setting not set.");
				connectionSuccess = false;
			}
			if (!connectionSuccess) {
				bool adetect = (bool)autodetect;
				JArray addresses = (JArray)serverAddress;
				if (!adetect && addresses.Count == 0) {
					log.Error("At least one server address must be set if autodetect is disabled");
					connectionSuccess = false;
				}
			}

			//Check performance settings
			JObject perfSettings = settings["performanceSettings"].ToObject<JObject>();
			bool performanceSuccess = true;

			perfSettings.TryGetValue("workerThreadCount", out JToken workerThreadCount);
			if (workerThreadCount == null || workerThreadCount.Type != JTokenType.Integer) {
				log.Error("Worker Thread Count setting not set.");
				performanceSuccess = false;
			}

			//Check authentication settings
			JObject authSettings = settings["authenticationSettings"].ToObject<JObject>();
			bool authenticationSuccess = true;

			authSettings.TryGetValue("expiration", out JToken expiration);
			if (expiration == null || expiration.Type != JTokenType.Integer) {
				log.Error("Token expiration setting not set.");
				authenticationSuccess = false;
			} else {
				int exp = (int)expiration;
				if (exp < 1) {
					log.Error("Token expiration must be at least 1");
					authenticationSuccess = false;
				} else if (exp < 7200) {
					log.Warning($"Token expiration is set to {exp}. This may increase server load.");
				}
			}

			authSettings.TryGetValue("usernameRequirements", out JToken usernameRequirementsValue);
			if (usernameRequirementsValue == null || usernameRequirementsValue.Type != JTokenType.Array) {
				log.Error("Username requirements not set.");
				authenticationSuccess = false;
			} else {
				foreach(JToken filter in usernameRequirementsValue) {
					if(filter.Type != JTokenType.Object) {
						authenticationSuccess = false;
						break;
					} else {
						JObject val = (JObject)filter;
						val.TryGetValue("length", out JToken length);
						val.TryGetValue("regex", out JToken regex);
						if((length != null && length.Type != JTokenType.Integer) || (regex != null && regex.Type != JTokenType.String)) {
							authenticationSuccess = false;
							break;
						}
					}
				}
				if (!authenticationSuccess) {
					log.Error("One or more username requirement filters is invalid.");
				}
			}
			

			//If all tests passed, return the settings JObject. Otherwise, return
			if (
				!databaseSuccess ||
				!connectionSuccess ||
				!performanceSuccess ||
				!authenticationSuccess
			) {
				return null;
			} else {
				return settings;
			}
		}
	}
}
