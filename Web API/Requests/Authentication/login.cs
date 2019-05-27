using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		/// <summary>
		/// Handles requests with requestType "login".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		[IgnoreUserToken]
		public JObject login(JObject request) {
			//Verify user details
			request.TryGetValue("username", out JToken usernameValue);
			request.TryGetValue("password", out JToken passwordValue);
			if (usernameValue.Type == JTokenType.Null || passwordValue.Type == JTokenType.Null) {
				return Templates.MissingArguments("username, password");
			}
			string username = usernameValue.ToString();
			string password = passwordValue.ToString();

			User user = GetObject<User>(username, "Username");
			if (user == null || user.Password != password) {
				return Templates.InvalidLogin;
			}

			long token = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			user.Token = token;
			user.Update(Connection);

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
