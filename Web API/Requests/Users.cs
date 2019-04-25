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
		public static JObject login(JObject request){
			//If the requestType isn't "login", throw an exception.
			if(request["requestType"].ToString() != "login"){
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//Verify user details
			String password = request["requestData"]["password"].ToString();
			String username = request["requestData"]["username"].ToString();
			IEnumerable<User> selection = wrapper.Select<User>();
			bool loginSuccessful = false;
			foreach(User user in selection){
				if(user.Password == password && user.Username == username){
					loginSuccessful = true;
				}
			}

			//Create + return response object
			JObject response = new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
					{"loginSuccesful", loginSuccessful },
				}}
			};
			if(loginSuccessful){
				response["requestData"]["userToken"] = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
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
		public static JObject registerUser(JObject request){
			//If the requestType isn't "registerUser", throw an exception.
			if (request["requestType"].ToString() != "registerUser") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//TODO Registration code
			bool registerUserSuccessful = true;

			return new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
					{"registerUserSuccessful", registerUserSuccessful},
					{"userToken", (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds}
				}}
			};
		}
	}
}
