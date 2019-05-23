using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using static API.Requests.RequestMethodAttributes;
using static API.Requests.Requests;


namespace API.Requests {
	static partial class RequestMethods {
		/// <summary>
		/// Handles requests with requestType "login".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>

		[skipTokenVerification]
		public static JObject login(JObject request) {
			//Verify user details
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("username", out JToken usernameValue);
			requestData.TryGetValue("password", out JToken passwordValue);
			if (usernameValue.Type == JTokenType.Null || passwordValue.Type == JTokenType.Null) {
				return Templates.MissingArguments("username, password");
			}
			string username = usernameValue.ToString();
			string password = passwordValue.ToString();

			User user = getObject<User>(username);
			if (user == null || user.Password != password) {
				return Templates.InvalidLogin;
			}

			long token = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			user.Token = token;
			user.Update(wrapper);

			//Create + return response object
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", new JObject() {
					{"token", token},
					{"permissionLevel", (int)user.Permission}
				}}
			};
			return response;
		}
	}
}
