using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Collaborator)]
		public JObject updateProductCategory(JObject request) {

			//Validate arguments
			string categoryID;
			string newCategoryID = null;
			JObject names = null;

			request.TryGetValue("categoryID", out JToken categoryIDValue);
			request.TryGetValue("newCategoryID", out JToken newCategoryIDValue);
			request.TryGetValue("name", out JToken nameValue);
			if (categoryIDValue == null || categoryIDValue.Type != JTokenType.String) {
				return Templates.MissingArguments("categoryID");
			} else {
				categoryID = categoryIDValue.ToObject<string>();
				if (categoryID == "default" || categoryID == "uncategorized") {
					return Templates.InvalidArgument("categoryID");
				}
			}
			if (newCategoryIDValue != null && newCategoryIDValue.Type == JTokenType.String) {
				newCategoryID = newCategoryIDValue.ToObject<string>();
			}
			if (nameValue != null && nameValue.Type == JTokenType.Object) {
				names = nameValue.ToObject<JObject>();
			}

			//Get product, if it exists
			ProductCategory category = GetObject<ProductCategory>(categoryID);
			if (category == null) {
				return Templates.NoSuchProductCategory(categoryID);
			}

			///////////////LanguageItem
			//Edit the LanguageItem if needed;
			LanguageItem item = category.GetName(Connection);
			if (names != null) {
				if (names.TryGetValue("en", out JToken enValue)) {
					if (enValue.Type == JTokenType.String) {
						item.en = enValue.ToObject<string>();
					}
				}
				if (names.TryGetValue("nl", out JToken nlValue)) {
					if (nlValue.Type == JTokenType.String) {
						item.nl = nlValue.ToObject<string>();
					}
				}
				if (names.TryGetValue("ar", out JToken arValue)) {
					if (arValue.Type == JTokenType.String) {
						item.ar = arValue.ToObject<string>();
					}
				}
				item.Update(Connection);
			}

			//If a new product ID was specified, check if it already exists. If it doesn't, change the product ID.
			if (newCategoryID != null) {
				ProductCategory newProduct = GetObject<ProductCategory>(newCategoryID);
				if (newProduct != null) {
					return Templates.AlreadyExists(categoryID);
				} else {
					item.Id = newCategoryID + "_name";
					item.Update(Connection);
					category.Name = item.Id;
					category.UpdateTrace();
					category.Id = newCategoryID;
				}
			}

			category.Update(Connection);

			//Create response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};
		}
	}
}