using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static API.Requests.RequestMethodAttributes;
using static API.Requests.Requests;

namespace API.Requests {
    static partial class RequestMethods {
        /// <summary>
        /// Handles requests with requestType "registerUser".
        /// </summary>
        /// <param name="request">The JObject containing the request received from the client.</param>
        /// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
        [skipTokenVerification]
        public static JObject registerUser(JObject request) {
            //Verify user details
            JObject requestData = request["requestData"].ToObject<JObject>();
            requestData.TryGetValue("username", out JToken usernameValue);
            requestData.TryGetValue("password", out JToken passwordValue);
            if (usernameValue == null || passwordValue == null || usernameValue.Type == JTokenType.Null || passwordValue.Type == JTokenType.Null) {
				return Templates.MissingArguments("username, password");
            }
            string username = usernameValue.ToString();
            string password = passwordValue.ToString();

            //Check if username already exists
            if (getUser(username) != null) {
				return Templates.AlreadyExists(username);
            }

            //Check if password is a SHA-512 hash.
            //This checks whether the password string is the correct length for a SHA-512 hash, and if it is a proper hexadecimal number.
            //It's possible for people directly calling the API to create a user with a password that wasn't salted with their username (should we fix this?), but I doubt anyone would do that.
            //Also regex is weird and I do not like it.
            if (password.Length != 128 && !System.Text.RegularExpressions.Regex.IsMatch(password, @"\A\b[0-9a-fA-F]+\b\Z")) {
				return Templates.InvalidPassword;
            }

			long token = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			User user = new User(username, password, token, User.UserPermission.User);
			user.Upload(wrapper);

			//Create response object
			JObject response = new JObject() {
                {"userToken", (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds },
                { "permissionLevel",  0},
                {"reason", null},
            };

            return response;
        }
    }
}