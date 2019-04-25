using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
	static partial class RequestMethods {
		public static JObject getProduct(JObject request) {
			//If the requestType isn't "login", throw an exception.
			if (request["requestType"].ToString() != "getProduct") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//TODO Add getProduct method

			return new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
				}}
			};
		}

		public static JObject updateProduct(JObject request) {
			//If the requestType isn't "login", throw an exception.
			if (request["requestType"].ToString() != "updateProduct") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//TODO Add updateProduct method

			return new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
				}}
			};
		}
	}
}