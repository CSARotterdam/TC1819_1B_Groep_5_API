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
				{"requestID", request["requestID"].ToString()},
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

			//Check if password meets criteria
			bool invalidPassword = false;
			if (password.Length < 10) {
				invalidPassword = true;
			}

			//TODO register user

			//Create + return response object
			JObject response = new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
					{"registerUserSuccessful", registerUserSuccessful},
					{"userToken", null},
					{"usernameReason", null},
					{"passwordReason", null}
				}}
			};
			if (registerUserSuccessful) {
				response["requestData"]["userToken"] = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			} else {
				if (usernameExists) {
					response["requestData"]["usernameReason"] = "User already exists.";
				}
				if (invalidPassword) {
					response["requestData"]["passwordReason"] = "Password too short.";
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
