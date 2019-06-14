using Newtonsoft.Json.Linq;

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
		public static JObject InvalidRequestType(string message = null) {
			return new JObject() {
				{"reason", "InvalidRequestType" },
				{"message", message },
			};
		}

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
		public static JObject NoSuchProduct(string message = null) {
			return new JObject() {
				{"reason", "NoSuchProduct" },
				{"message", message },
			};
		}

		/// <summary>
		/// Sent when a nonexistent product item was requested.
		/// </summary>
		public static JObject NoSuchProductItem(string message = null) {
			return new JObject() {
				{"reason", "NoSuchProductItem" },
				{"message", message },
			};
		}

		/// <summary>
		/// Sent when a nonexistent loan was requested
		/// </summary>
		/// <param name="loanId">The ID of the LoanItem</param>
		/// <returns></returns>
		internal static JObject NoSuchLoan(string loanId = null) {
			return new JObject() {
				{"reason", "NoSuchLoan" },
				{"message", loanId },
			};
		}

		/// <summary>
		/// Sent when a nonexistent product category was requested.
		/// </summary>
		public static JObject NoSuchProductCategory(string message = null) {
			return new JObject() {
				{"reason", "NoSuchProductCategory" },
				{"message", message },
			};
		}

		/// <summary>
		/// Sent when a nonexistent user was requested.
		/// </summary>
		public static JObject NoSuchUser(string message = null) {
			return new JObject() {
				{"reason", "NoSuchUser" },
				{"message", message },
			};
		}

		/// <summary>
		/// Sent when a user tries to add a new object (users, products, product categories, etc), but an object already existed with the specified ID.
		/// </summary>
		public static JObject AlreadyExists(string message = null) {
			return new JObject() {
				{"reason", "AlreadyExists" },
				{"message", message },
			};
		}

		/// <summary>
		/// Sent when a client attempts to use a password with an invalid format.
		/// Passwords must be 128 character hexadecimal strings (@"\A\b[0-9a-fA-F]+\b\Z")
		/// </summary>
		public static JObject InvalidPassword = new JObject() {
			{"reason", "InvalidPassword" }
		};

		/// <summary>
		/// Sent when a client attempts to use a username with an invalid format.
		/// Usernames must match at least one of the filters configured in config.json, if any.
		/// </summary>
		public static JObject InvalidUsername = new JObject() {
			{"reason", "InvalidUsername" }
		};

		/// <summary>
		/// Used when a delete request cannot finish because of false prerequisites.
		/// </summary>
		/// <param name="message">An optional message to attach.</param>
		public static JObject CannotDelete(string message) => new JObject() {
				{"reason", "CannotDelete"},
				{"message", message}
			};

		/// <summary>
		/// Sent when a client sends a request without including the data necessary to fullfil it.
		/// Also thrown when the arguments are the wrong type, as these are ignored and therefore assumed to be missing.
		/// </summary>
		/// <param name="message">The arguments that were required for the requested function.</param>
		public static JObject MissingArguments(string message) {
			return new JObject() {
				{"reason", "MissingArguments"},
				{"message", message}
			};
		}

		/// <summary>
		/// Sent when a client sends a request without including several required parameters.
		/// </summary>
		/// <param name="args">A set of parameter names</param>
		public static JObject MissingArguments(params string[] args) => MissingArguments(string.Join(", ", args));

		/// <summary>
		/// Sent when a request from a client is missing essential data, preventing it from being parsed.
		/// </summary>
		/// <param name="message">The arguments that were required for the requested function.</param>
		public static JObject MalformedRequest(string message) {
			return new JObject() {
				{"reason", "MalformedRequest"},
				{"message", message}
			};
		}

		/// <summary>
		/// Sent when a client's request could not be fulfilled due to a server error.
		/// </summary>
		/// <param name="message">The error that caused this failure.</param>
		/// <returns></returns>
		public static JObject ServerError(string message = null) {
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
		public static JObject InvalidArgument(string argName) => InvalidArguments(argName);

		/// <summary>
		/// Sent when a client sends a request with arguments set to unacceptable values.
		/// Examples include out of range integers, strings with an incorrect length, etc.
		/// </summary>
		/// <param name="argNames">The names of the arguments with invalid values.</param>
		/// <returns>The "Invalid Arguments" response template.</returns>
		public static JObject InvalidArguments(params string[] argNames) {
			return new JObject() {
				{"reason", "InvalidArguments"},
				{"message", string.Join(", ", argNames) }
			};
		}

		/// <summary>
		/// Error response when a request handler tried to retrieve items of a certain product type, but none exist.
		/// </summary>
		/// <param name="message">An optional message.</param>
		public static JObject NoItemsForProduct(string message = null) {
			return new JObject() {
				{"reason", "NoItemsForProduct"},
				{"message", message }
			};
		}

		/// <summary>
		/// Sent when the server tries to create a loan, but it failed.
		/// </summary>
		/// <param name="message">An optional message.</param>
		public static JObject ReservationFailed(string message = null) {
			return new JObject() {
				{"reason", "ReservationFailed"},
				{"message", message }
			};
		}

		/// <summary>
		/// Sent when the client requested a resize to the loan's timespan, but this couldn't be done due to other conflicting loans.
		/// </summary>
		/// <param name="message">An optional message.</param>
		/// <param name="amount"></param>
		/// <returns>The amount of loanItems that overlapped</returns>
		public static JObject LoanResizeFailed(string message = null, int? amount = null) {
			return new JObject() {
				{"reason", "LoanResizeFailed"},
				{"message", message },
				{"amount", amount }
			};
		}

		/// <summary>
		/// Sent when the client requested a resize to the loan's timespan, but the specified loan has already expired.
		/// </summary>
		/// <param name="message">An optional message.</param>
		public static JObject LoanExpired(string message = null) {
			return new JObject() {
				{"reason", "LoanResizeFailed"},
				{"message", message }
			};
		}

		/// <summary>
		/// Sent when the client requested a delete to a loan, but the loan has already started.
		/// </summary>
		/// <param name="message">An optional message.</param>
		public static JObject LoanAlreadyStarted(string message = null) {
			return new JObject() {
				{"reason", "LoanAlreadyStarted"},
				{"message", message }
			};
		}
	}
}
