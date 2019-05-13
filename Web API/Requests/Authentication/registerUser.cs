using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static API.Requests.RequestMethodAttributes;

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
            if (usernameValue.Type == JTokenType.Null || passwordValue.Type == JTokenType.Null) {
                return Templates.MissingArguments;
            }
            string username = usernameValue.ToString();
            string password = passwordValue.ToString();

            //Check if username already exists
            bool usernameExists = false;
            if (getUser(username) != null) {
                usernameExists = true;
            }

            //Check if password is a SHA-512 hash.
            //This checks whether the password string is the correct length for a SHA-512 hash, and if it is a proper hexadecimal number.
            //It's possible for people directly calling the API to create a user with a password that wasn't salted with their username (should we fix this?), but I doubt anyone would do that.
            //Also regex is weird and I do not like it.
            bool invalidPassword = false;
            if (password.Length != 128 && !System.Text.RegularExpressions.Regex.IsMatch(password, @"\A\b[0-9a-fA-F]+\b\Z")) {
                invalidPassword = true;
            }

            //Create response object
            JObject response = new JObject() {
                {"registerUserSuccessful", null},
                {"userToken", null},
                {"permissionLevel", null},
                {"reason", null},
            };

            //If the above checks passed, register the user.
            if (!invalidPassword && !usernameExists) {
                long token = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                User user = new User(username, password, token, User.UserPermission.User);
                user.Upload(wrapper);
                response["requestData"]["registerUserSuccessful"] = true;
                response["requestData"]["token"] = token;
                response["requestData"]["permissionLevel"] = 0;
            } else {
                if (usernameExists) {
                    response["requestData"]["reason"] = "User already exists.";
                    response["requestData"]["registerUserSuccessful"] = false;
                } else if (invalidPassword) {
                    response["requestData"]["reason"] = "Password not valid.";
                    response["requestData"]["registerUserSuccessful"] = false;
                }
            }

            return response;
        }
    }
}