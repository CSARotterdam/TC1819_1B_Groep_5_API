using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Collaborator)]
		public JObject deleteProductCategory(JObject request) {
			//Get arguments
			request.TryGetValue("categoryID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			}

			// Prepare values
			string categoryID = idValue.ToString();

			//Check if product exists
			ProductCategory category = GetObject<ProductCategory>(categoryID);
			if (category == null) {
				return Templates.NoSuchProductCategory(categoryID);
			}
			
			// Delete category and relate
			category.Delete(Connection);
			category.GetName(Connection).Delete(Connection);

			//Create base response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};

		}
	}
}