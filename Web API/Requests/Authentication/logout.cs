using Newtonsoft.Json.Linq;

namespace API.Requests {
	abstract partial class RequestHandler {
		/// <summary>
		/// Handles requests with requestType "logout".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A <see cref="JObject"/> containing the request response, which can then be sent to the client.</returns>
		public JObject logout(JObject _) {
			//Create response object
			JObject response = new JObject() {
				{"success", true },
				{"reason", null }
			};

			//Log the user out
			CurrentUser.Token = 0;
			CurrentUser.Update(Connection);
			return response;
		}
	}
}