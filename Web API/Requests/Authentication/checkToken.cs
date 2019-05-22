using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {
		/// <summary>
		/// Handles requests with requestType "checkToken".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>

		public static JObject checkToken(JObject request) {
			return new JObject() {
				{"reason", null },
			};
		}
	}
}
