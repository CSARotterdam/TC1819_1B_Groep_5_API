using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;
using static API.Requests.Requests;

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
			User user = getObject<User>(username);
			if (user == null) {
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
