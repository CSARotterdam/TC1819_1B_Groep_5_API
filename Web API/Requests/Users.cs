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
			foreach (User user in selection) {
				if (user.Password == password && user.Username == username) {
					loginSuccessful = true;
					break;
				}
			}

			//Create + return response object
			JObject response = new JObject() {
				{"requestData", new JObject(){
					{"loginSuccesful", loginSuccessful },
					{"userToken", null},
					{"reason", null }
				}}
			};
			if (loginSuccessful) {
				response["requestData"]["userToken"] = generateUserToken(username);
			} else {
				response["requestData"]["reason"] = "Incorrect username/password";
			}
			return response;
		}







		/// <summary>
		/// Handles requests with requestType "registerUser".
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static JObject registerUser(JObject request) {
			//If the requestType isn't "registerUser", throw an exception.
			if (request["requestType"].ToString() != "registerUser") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			String password = request["requestData"]["password"].ToString();
			String username = request["requestData"]["username"].ToString();

			bool registerUserSuccessful = false;

			//Check if username already exists
			bool usernameExists = false;
			IEnumerable<User> selection = wrapper.Select<User>();
			foreach (User user in selection) {
				if (user.Username == username) {
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

            //If the above checks passed, register the user.
            if(!invalidPassword && !usernameExists){
                User user = new User(username, password, User.UserPermission.User);
                user.Upload(wrapper);
                registerUserSuccessful = true;
            }

			//Create + return response object
			JObject response = new JObject() {
				{"requestData", new JObject(){
					{"registerUserSuccessful", registerUserSuccessful},
					{"userToken", null},
					{"reason", null},
				}}
			};
			if (registerUserSuccessful) {
				response["requestData"]["userToken"] = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			} else {
				if (usernameExists) {
					response["requestData"]["reason"] = "User already exists.";
				} else if (invalidPassword) {
                    response["requestData"]["reason"] = "Password not valid.";
                }
			}

			return response;
		}







		public static JObject logout(JObject request) {
			//If the requestType isn't "login", throw an exception.
			if (request["requestType"].ToString() != "logout") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//TODO 

			//Create + return response object
			//Don't need any requestData for this one; the response itself serves as a confirmation.
			return new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
				}}
			};
		}
	}
}
