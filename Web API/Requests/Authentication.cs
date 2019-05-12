using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class RequestMethods {
		/// <summary>
		/// Handles requests with requestType "login".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		public static JObject login(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
			//If the requestType isn't "login", throw an exception.
			if (request["requestType"].ToString() != "login") {
                return Templates.InvalidRequestType;
			}

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
            if(user != null && user.Password == password) {
                loginSuccessful = true;
            }

            //Create + return response object
            JObject response = new JObject() {
                {"requestData", new JObject(){
                    {"loginSuccesful", loginSuccessful },
                    {"token", null},
                    {"permissionLevel", -1},
					{"reason", null }
				}}
			};
			if (loginSuccessful) {
                long token = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                user.Token = token;
                user.Update(wrapper);
                response["requestData"]["token"] = token;
                response["requestData"]["permissionLevel"] = (int)user.Permission;
			} else {
				response["requestData"]["reason"] = "Incorrect username/password";
			}
			return response;
		}

        /// <summary>
        /// Handles requests with requestType "registerUser".
        /// </summary>
        /// <param name="request">The JObject containing the request received from the client.</param>
        /// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
        public static JObject registerUser(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
            //If the requestType isn't "registerUser", throw an exception.
            if (request["requestType"].ToString() != "registerUser") {
                return Templates.InvalidRequestType;
            }

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
			if(getUser(username) != null) {
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
                {"requestData", new JObject(){
                    {"registerUserSuccessful", null},
                    {"userToken", null},
                    {"permissionLevel", null},
                    {"reason", null},
                }}
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

        /// <summary>
		/// Handles requests with requestType "logout".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A <see cref="JObject"/> containing the request response, which can then be sent to the client.</returns>
		public static JObject logout(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
            //If the requestType isn't "login", throw an exception.
            if (request["requestType"].ToString() != "logout") {
                return Templates.InvalidRequestType;
            }

            //Verify user details
            JObject requestData = request["requestData"].ToObject<JObject>();
            requestData.TryGetValue("username", out JToken usernameValue);
            requestData.TryGetValue("token", out JToken tokenValue);
            if (usernameValue.Type == JTokenType.Null || tokenValue.Type == JTokenType.Null) {
                return Templates.MissingArguments;
            }
            string username = usernameValue.ToString();
            long token = tokenValue.ToObject<long>();

            User user = getUser(username);

            //Create response object
            JObject response = new JObject() {
                {"requestData", new JObject(){
                    {"success", true },
                    {"reason", null }
				}}
			};

            //If token is valid, log the user out. Otherwise, return an error response
            if (user == null) {
                response["requestData"]["success"] = false;
                response["requestData"]["reason"] = "No such user";

            } else if (checkToken(user, token)) {
                user.Token = 0;
                user.Update(wrapper);

            } else {
                return Templates.ExpiredToken;
            }
            return response;
        }

        /// <summary>
        /// Given a user and token, checks if the token is valid.
        /// </summary>
        /// <param name="user">A user object</param>
        /// <param name="tokenRaw">A long containing the raw token</param>
        /// <returns>Bool, which is true if the token is valid.</returns>
        private static bool checkToken(User user, long tokenRaw) {
            if (user.Token != tokenRaw) {
                return false;
            }

            System.DateTime token = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            token = token.AddSeconds(tokenRaw).ToLocalTime();
            return !((DateTime.Today - token).TotalSeconds > (double)Program.Settings["authenticationSettings"]["expiration"]);
        }

        private static User getUser(string username) {
            List<User> selection = wrapper.Select<User>(new MySqlConditionBuilder()
                   .Column("Username")
                   .Equals()
                   .Operand(username, MySql.Data.MySqlClient.MySqlDbType.VarChar)
            ).ToList();

            if (selection.Count == 0) {
                return null;
            } else {
                return selection[0];
            }
        }
	}
}
