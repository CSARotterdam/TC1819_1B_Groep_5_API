using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
	static partial class RequestMethods {
		/// <summary>
		/// Handles requests with requestType "login".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		public static JObject login(JObject request) {
			//If the requestType isn't "login", throw an exception.
			if (request["requestType"].ToString() != "login") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//Verify user details
			String password = request["requestData"]["password"].ToString();
			String username = request["requestData"]["username"].ToString();
			IEnumerable<User> selection = wrapper.Select<User>();
			bool loginSuccessful = false;
            User selectedUser = null;
			foreach (User user in selection) {
                if (user.Password == password && user.Username == username) {
					loginSuccessful = true;
                    selectedUser = user;
                    break;
				}
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
                selectedUser.Token = token;
                selectedUser.Update(wrapper);
                response["requestData"]["token"] = token;
                response["requestData"]["permissionLevel"] = (int)selectedUser.Permission;
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
			//If the requestType isn't "registerUser", throw an exception.
			if (request["requestType"].ToString() != "registerUser") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

            String password = "";
            String username = "";
            try {
                password = request["requestData"]["password"].ToString();
                username = request["requestData"]["username"].ToString();
            } catch(ArgumentException) {

            }

			//Check if username already exists
			bool usernameExists = false;
			IEnumerable<User> selection = wrapper.Select<User>();
			foreach (User u in selection) {
				if (u.Username == username) {
					usernameExists = true;
					break;
				}
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
            if (!invalidPassword && !usernameExists){
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
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		public static JObject logout(JObject request) {
			//If the requestType isn't "login", throw an exception.
			if (request["requestType"].ToString() != "logout") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

            //Find the correct user
            String username = "";
            long token = 0;
            try {
                username = request["requestData"]["username"].ToString();
                token = request["requestData"]["token"].ToObject<long>();
            } catch (ArgumentException) {

            }
            IEnumerable<User> selection = wrapper.Select<User>();
            User selectedUser = null;
            foreach (User user in selection) {
                if (user.Username == username) {
                    selectedUser = user;
                    break;
                }
            }

            //Create response object
            JObject response = new JObject() {
                {"requestData", new JObject(){
                    {"success", true },
                    {"reason", null }
				}}
			};

            //If token is valid, log the user out. Otherwise, return an error response
            if (checkToken(selectedUser, token)) {
                selectedUser.Token = 0;
                selectedUser.Update(wrapper);
            } else {
                response["requestData"]["success"] = false;
                response["requestData"]["reason"] = "Invalid or expired token";
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
	}
}
