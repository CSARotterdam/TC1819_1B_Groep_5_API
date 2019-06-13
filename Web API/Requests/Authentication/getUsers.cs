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
			//Get all users
			string[] columns = new string[] { "username", "permissions"};
			List<string[]> userdata = Connection.Select<User>(columns).Select(x => x.Cast<string>().ToArray()).ToList();

			//Create response list
			JArray users = new JArray();
			foreach(string[] user in userdata) {
				users.Add(new JObject() {
					{"username", user[0] },
					{"permission", (int)Enum.Parse(typeof(UserPermission), user[1]) }
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
