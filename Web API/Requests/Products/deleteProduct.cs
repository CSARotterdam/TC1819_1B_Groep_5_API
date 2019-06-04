using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject deleteProduct(JObject request) {
			//Get arguments
			request.TryGetValue("productID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			}

			// Prepare values
			string productID = idValue.ToString();


			//Check if product exists
			Product product = GetObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			// Check if items or acquired loans exist
			var condition = new MySqlConditionBuilder();

			// deltete stuffz
			product.Delete(Connection);
			if (product.Name != "default") {
				product.GetImage(Connection)?.Delete(Connection);
			}
			if (product.Name != "0") {
				product.GetName(Connection)?.Delete(Connection);
			}

			//Create base response
			return new JObject() {
				{"reason", null },
			};

		}
	}
}