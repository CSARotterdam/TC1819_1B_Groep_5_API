using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		//Note: Uses its own permission check, because while only admins can edit every user, the rest can edit their own password.
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject updateUser(JObject request) {
			//Get arguments
			string username;
			string password;
			int permission = -2;
			request.TryGetValue("username", out JToken usernameValue);
			request.TryGetValue("password", out JToken passwordValue);
			request.TryGetValue("permission", out JToken permissionValue);
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
			User currentUser = GetObject<User>(request["username"].ToObject<string>(), "Username");
			if (currentUser.Username != username) {
				if (currentUser.Permission != UserPermission.Admin) {
					return Templates.AccessDenied;
				}
			} else if (permission != -2) {
				return Templates.AccessDenied;
			}

			//Get user
			User user = GetObject<User>(username, "Username");
			if (user == null) {
				return Templates.NoSuchUser(username);
			}

			//Edit user
			if (password != null) {
				user.Password = password;
			}
			if (permission != -2) {
				user.Permission = (UserPermission)permission;
			}
			user.Update(Connection);

			//Create response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}
