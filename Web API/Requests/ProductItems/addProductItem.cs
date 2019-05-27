using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject addProductItem(JObject request) {
			//Get arguments
			string productID;
			request.TryGetValue("productID", out JToken productIDValue);
			if (productIDValue == null || productIDValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID, categoryID, manufacturer, name");
			} else {
				productID = productIDValue.ToObject<string>();
			}

			//Check if product exists
			Product product = GetObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			//Create productItem
			ProductItem item = new ProductItem(null, productID);
			item.Upload(Connection);

			//Create response
			return new JObject() {
				{"reason", null },
				{"productItemID",  item.Id}
			};
		}
	}
}