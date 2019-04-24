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
			//If the requestType is 
			if(request["requestType"].ToString() != "login"){
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			String password = request["requestData"]["password"].ToString();
			String username = request["requestData"]["username"].ToString();

			//TODO: Actually implement login
			bool loginSuccessful = false;
			if (username == "test" && password == "e9e633097ab9ceb3e48ec3f70ee2beba41d05d5420efee5da85f97d97005727587fda33ef4ff2322088f4c79e8133cc9cd9f3512f4d3a303cbdb5bc585415a00") {
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
