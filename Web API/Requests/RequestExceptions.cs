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

	public class MissingRequestDataException : Exception {
		/// <summary>
		/// Exception thrown when a request is missing its requestData value.
		/// </summary>
		/// <param name="message"></param>
		public MissingRequestDataException()
			: base(string.Format("Received request with missing requestData")) {
		}
	}
}
