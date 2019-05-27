using Newtonsoft.Json.Linq;

namespace API.Requests {
	abstract partial class RequestHandler {
		/// <summary>
		/// Handles requests with requestType "checkToken".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>

		public static JObject checkToken(JObject _) {
			return new JObject() {
				{"reason", null },
			};
		}
	}
}
