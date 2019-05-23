using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace API {
	class Config {
		private static readonly string defaultJson = @"{
			'databaseSettings': {
				'username': null,
				'password': null,
				'serverAddress': null,
				'database': null,
				'connectionTimeout':-1,
				'persistLogin': true,
			},
			'connectionSettings': {
				'autodetect': true,
				'serverAddresses': [
					'localhost'
				]
			},
			'performanceSettings':{
				'workerThreadCount': 5,
			},
            'authenticationSettings':{
                'expiration': 7200
            }
		}";

		public static dynamic loadConfig() {
			string filename = "config.json";
			dynamic Settings;

			if (!File.Exists(filename)) {
				Settings = JObject.Parse(defaultJson);
				using (JsonTextWriter writer = new JsonTextWriter(File.CreateText(filename))) {
					writer.Formatting = Formatting.Indented;
					Settings.WriteTo(writer);
				}

			} else {
				Settings = JObject.Parse(File.ReadAllText(filename));
			}
			return Settings;
		}
	}
}
