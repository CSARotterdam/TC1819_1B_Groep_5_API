using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static API.Requests.Requests;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {

		[verifyPermission(User.UserPermission.Admin)]
		public static JObject deleteUser(JObject request) {
			//Get arguments
			string username;
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("username", out JToken usernameValue);
			if (usernameValue == null || usernameValue.Type != JTokenType.String) {
				return Templates.MissingArguments("username");
			} else {
				username = usernameValue.ToObject<string>();
			}

			//Check if user exists
			User user = getUser(username);
			if(user == null) {
				return Templates.NoSuchUser(username);
			}

			user.Delete(wrapper);
			//Create base response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}
