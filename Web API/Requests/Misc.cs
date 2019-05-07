using MySQLWrapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
    static partial class RequestMethods {
        public static TechlabMySQL wrapper;

        /// <summary>
		/// Handles requests with requestType "getImage".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		public static JObject getImage(JObject request) {
            //If the requestType isn't "login", throw an exception.
            if (request["requestType"].ToString() != "login") {
                throw new InvalidRequestTypeException(request["requestType"].ToString());
            }

            //TODO getImage code

            JObject response = new JObject() {
                {"requestData", new JObject(){
                    {""} //TODO Response data
                }}
            };
            return response;
        }
    }

    public class MissingArgumentsException : Exception {
        public MissingArgumentsException() {
        }

        public MissingArgumentsException(string message)
            : base(message) {
        }

        public MissingArgumentsException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
