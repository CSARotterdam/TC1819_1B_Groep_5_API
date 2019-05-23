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
		//Note: Uses its own permission check, because while only admins can edit every user, users can edit their own password.
		public static JObject updateUser(JObject request) {
			//Get arguments
			string username;
			string password;
			int permission = -2;
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("username", out JToken usernameValue);
			requestData.TryGetValue("password", out JToken passwordValue);
			requestData.TryGetValue("permission", out JToken permissionValue);
			if (usernameValue == null || usernameValue.Type != JTokenType.String) {
				return Templates.MissingArguments("username");
			} else {
				username = usernameValue.ToObject<string>();
			}
			if (passwordValue == null || passwordValue.Type != JTokenType.String) {
				password = null;
			} else {
				password = passwordValue.ToObject<string>();
				if (password.Length != 128 && !System.Text.RegularExpressions.Regex.IsMatch(password, @"\A\b[0-9a-fA-F]+\b\Z")) {
					return Templates.InvalidPassword;
				}
			}
			if (permissionValue != null && permissionValue.Type == JTokenType.Integer) {
				permission = permissionValue.ToObject<int>();
			}

			//Check permission
			User currentUser = getObject<User>(request["username"].ToObject<string>());
			if (currentUser.Username != username) {
				if(currentUser.Permission != User.UserPermission.Admin) {
					return Templates.AccessDenied;
				}
			} else if(permission != -2) {
				return Templates.AccessDenied;
			}

			//Get user
			User user = getObject<User>(username);
			if(user == null) {
				return Templates.NoSuchUser;
			}

			//Edit user
			if(password != null) {
				user.Password = password;
			}
			if(permission != -2) {
				user.Permission = (User.UserPermission)permission;
			}
			user.Update(wrapper);

			//Create response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}
