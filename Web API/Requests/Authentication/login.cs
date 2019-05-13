using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static API.Requests.RequestMethodAttributes;

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
                return Templates.MissingArguments;
            }
            string username = usernameValue.ToString();
            string password = passwordValue.ToString();

            bool loginSuccessful = false;
            User user = getUser(username);
            if (user != null && user.Password == password) {
                loginSuccessful = true;
            }

            //Create + return response object
            JObject response = new JObject() {
                {"loginSuccesful", loginSuccessful },
                {"token", null},
                {"permissionLevel", -1},
                {"reason", null }
            };
            if (loginSuccessful) {
                long token = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                user.Token = token;
                user.Update(wrapper);
                response["token"] = token;
                response["permissionLevel"] = (int)user.Permission;
            } else {
                response["reason"] = "Incorrect username/password";
            }
            return response;
        }
    }
}
