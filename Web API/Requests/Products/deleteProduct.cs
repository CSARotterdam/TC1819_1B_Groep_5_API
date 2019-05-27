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

			string productID = idValue.ToString();

			//Check if product exists
			Product product = GetObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			product.Delete(Connection);
			Image image = product.GetImage(Connection);
			if (image.Id != "default") {
				image.Delete(Connection);
			}
			LanguageItem name = product.GetName(Connection);
			if (name.Id != "0") {
				name.Delete(Connection);
			}

			//Create base response
			return new JObject() {
				{"reason", null },
			};

		}
	}
}