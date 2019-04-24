using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
	static class LoginRequest {
		/// <summary>
		/// Handles requests with requestType "login".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		public static JObject Login(JObject request){
			//If the requestType is 
			if(request["requestType"].ToString() != "login"){
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			String password = request["requestData"]["password"].ToString();
			String username = request["requestData"]["username"].ToString();

			//TODO: Actually implement login
			bool loginSuccessful = false;
			if (username == "test" && password == "[B@6ad6ff3") {
				loginSuccessful = true;
			}

			//Return response object
			return new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
					{"loginSuccesful", loginSuccessful }
				}}
			};
		}
	}
}
