using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject deleteProductItem(JObject request) {
			//Get arguments
			request.TryGetValue("productItemID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productItemID");
			}

			string productID = idValue.ToString();
			if (productID == "0") {
				return Templates.InvalidArgument("productID");
			}

			//Check if productItem exists
			ProductItem item = GetObject<ProductItem>(productID);
			if (item == null) {
				return Templates.NoSuchProduct(productID);
			}
			item.Delete(Connection);

			//Create base response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}