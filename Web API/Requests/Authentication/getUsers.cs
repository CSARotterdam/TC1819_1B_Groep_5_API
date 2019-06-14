using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject getUsers(JObject request) {
			request.TryGetValue("permission", out JToken permValue);
			request.TryGetValue("username", out JToken usernameValue);

			// Verify arguments
			int permission;
			string username;
			if (permValue != null && permValue.Type == JTokenType.Integer) {
				permission = permValue.ToObject<int>();
				if (permission <= -1 || permission >= 4) {
					return Templates.InvalidArguments("Permission out of range: " + permission);
				}
			} else {
				permission = -1;
			}			
			if (usernameValue != null && usernameValue.Type == JTokenType.String) {
				username = usernameValue.ToObject<string>();
			} else {
				username = null;
			}
				
			username += "%";

			//Construct query
			MySqlConditionBuilder query = new MySqlConditionBuilder();
			if(username != null) {
				query.Column("username")
				.Like(username);
			}
			if (permission != -1) {
				query.And()
				.Column("permissions")
				.Equals(permission, MySql.Data.MySqlClient.MySqlDbType.Int32);
			}
				
			//Get users
			List<User> userdata = Connection.Select<User>(query).ToList();

			//Create response list
			JArray users = new JArray();
			foreach(User user in userdata) {
				users.Add(new JObject() {
					{"username", user.Username },
					{"permission", (int)user.Permission }
				});
			}

			//Create response
			return new JObject() {
				{"reason", null },
				{"responseData", users }
			};
		}
	}
}
