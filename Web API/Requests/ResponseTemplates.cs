using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
    public static class Templates {
		/// <summary>
		/// Sent when a user is trying to call a function but doesn't have a valid token.
		/// </summary>
        public static JObject ExpiredToken = new JObject() {
            {"reason", "ExpiredToken"}
        };

		/// <summary>
		/// Sent when a user is trying to call a nonexistent function.
		/// </summary>
        public static JObject InvalidRequestType = new JObject() {
            {"reason", "InvalidRequestType"}
        };

		/// <summary>
		/// Sent when a user is trying to call a function without having enough permissions to do so.
		/// </summary>
        public static JObject AccessDenied = new JObject() {
            {"reason", "AccessDenied"}
        };

		/// <summary>
		/// Sent when a user is trying to login using incorrect user details.
		/// </summary>
        public static JObject InvalidLogin = new JObject() {
            {"reason" , "InvalidLogin"}
        };

		/// <summary>
		/// Sent when a nonexistent product was requested.
		/// </summary>
		public static JObject NoSuchProduct = new JObject() {
			{"reason", "NoSuchProduct" }
		};

		/// <summary>
		/// Sent when a nonexistent product item was requested.
		/// </summary>
		public static JObject NoSuchProductItem = new JObject() {
			{"reason", "NoSuchProductItem" }
		};

		/// <summary>
		/// Sent when a nonexistent product category was requested.
		/// </summary>
		public static JObject NoSuchProductCategory = new JObject() {
			{"reason", "NoSuchProductCategory" }
		};

		/// <summary>
		/// Sent when a nonexistent user was requested.
		/// </summary>
		public static JObject NoSuchUser = new JObject() {
			{"reason", "NoSuchUser" }
		};

		/// <summary>
		/// Sent when a user tries to add a new object (users, products, product categories, etc), but an object already existed with the specified ID.
		/// </summary>
		public static JObject AlreadyExists = new JObject() {
			{"reason", "AlreadyExists" }
		};


		/// <summary>
		/// Sent when a client sends a request without including the data necessary to fullfil it.
		/// Also thrown when the arguments are the wrong type, as these are ignored and therefore assumed to be missing.
		/// </summary>
		/// <param name="message">The arguments that were required for the requested function.</param>
		/// <returns></returns>
		public static JObject MissingArguments(string message) {
			return new JObject() {
				{"reason", "MissingArguments"},
				{"message", message}
			};
		}

		/// <summary>
		/// Sent when a client's request could not be fulfilled due to a server error.
		/// </summary>
		/// <param name="message">The error that caused this failure.</param>
		/// <returns></returns>
        public static JObject ServerError(string message) {
            return new JObject() {
                {"reason", "ServerError"},
                {"message", message}
            };
        }

		/// <summary>
		/// Sent when a client sends a request with an argument set to unacceptable values.
		/// Examples include out of range integers, strings with an incorrect length, etc.
		/// </summary>
		/// <param name="argName">The name of the argument with an invalid value.</param>
		/// <returns>The "Invalid Argument" response template.</returns>
		public static JObject InvalidArgument(string argName) {
			return new JObject() {
				{"reason", "InvalidArgument"},
				{"message", argName}
			};
		}

		/// <summary>
		/// Sent when a client sends a request with arguments set to unacceptable values.
		/// Examples include out of range integers, strings with an incorrect length, etc.
		/// </summary>
		/// <param name="argNames">The names of the arguments with invalid values.</param>
		/// <returns>The "Invalid Argument" response template.</returns>
		public static JObject InvalidArguments(params string[] argNames) {
			return new JObject() {
				{"reason", "InvalidArgument"},
				{"message", string.Join(", ", argNames) }
			};
		}
    }
}
