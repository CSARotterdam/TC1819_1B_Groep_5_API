using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
    static partial class RequestMethods {


        /// <summary>
        /// Handles requests with requestType "logout".
        /// </summary>
        /// <param name="request">The JObject containing the request received from the client.</param>
        /// <returns>A <see cref="JObject"/> containing the request response, which can then be sent to the client.</returns>
         
        public static JObject logout(JObject request) {
            //Verify user details
            JObject requestData = request["requestData"].ToObject<JObject>();
            requestData.TryGetValue("username", out JToken usernameValue);
            requestData.TryGetValue("token", out JToken tokenValue);
            if (usernameValue.Type == JTokenType.Null || tokenValue.Type == JTokenType.Null) {
				return Templates.MissingArguments("username, token");
            }
            string username = usernameValue.ToString();

            User user = getUser(username);

            //Create response object
            JObject response = new JObject() {
                {"success", true },
                {"reason", null }
            };

            //Log the user out
            user.Token = 0;
            user.Update(wrapper);
            return response;
        }
    }
}