using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
	public class InvalidRequestTypeException : Exception {
		/// <summary>
		/// Exception thrown when a request is missing a proper requestType value.
		/// Should only be thrown by requestHandlers.
		/// </summary>
		/// <param name="requestType"></param>
		public InvalidRequestTypeException(string requestType)
			: base(string.Format("Received request with invalid requestType: {0}", requestType)){
		}
	}

    public static class Templates {
        public static JObject MissingArguments = new JObject() {
            {"requestData", new JObject(){
                {"reason", "MissingArguments" }
            }}
        };
        public static JObject ExpiredToken = new JObject() {
            {"requestData", new JObject(){
                {"reason", "InvalidToken"}
            }}
        };
    }
}
