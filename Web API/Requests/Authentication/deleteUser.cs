using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject deleteUser(JObject request) {
			//Get arguments
			string username;
			request.TryGetValue("username", out JToken usernameValue);
			if (usernameValue == null || usernameValue.Type != JTokenType.String) {
				return Templates.MissingArguments("username");
			} else {
				username = usernameValue.ToObject<string>();
			}

			//Check if user exists
			User user = GetObject<User>(username, "Username");
			if (user == null) {
				return Templates.NoSuchUser(username);
			}

			//Create + return response object
			return new JObject() {
				{"reason", null },
				{"responseData", new JObject() }
			};
		}
	}
}
