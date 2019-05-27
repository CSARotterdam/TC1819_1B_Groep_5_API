using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Collaborator)]
		public JObject addProductCategory(JObject request) {
			//Get arguments
			string categoryID;
			request.TryGetValue("categoryID", out JToken categoryIDValue);
			request.TryGetValue("name", out JToken nameValue);
			if (categoryIDValue == null || categoryIDValue.Type != JTokenType.String ||
				nameValue == null || nameValue.Type != JTokenType.Object
			) {
				return Templates.MissingArguments("categoryID, name");
			} else {
				categoryID = categoryIDValue.ToObject<string>();
				if (categoryID == "default" || categoryID == "uncategorized") {
					return Templates.InvalidArgument("categoryID");
				}
			}

			//Get languages
			string en;
			string nl = null;
			string ar = null;
			JObject names = nameValue.ToObject<JObject>();
			names.TryGetValue("en", out JToken enValue);
			names.TryGetValue("nl", out JToken nlValue);
			names.TryGetValue("ar", out JToken arValue);
			if (enValue != null && enValue.Type == JTokenType.String) {
				en = names["en"].ToObject<string>();
			} else {
				return Templates.MissingArguments("en");
			}
			if (nlValue != null && nlValue.Type == JTokenType.String) {
				nl = names["nl"].ToObject<string>();
			}
			if (arValue != null && arValue.Type == JTokenType.String) {
				ar = names["ar"].ToObject<string>();
			}


			//Check if category already exists
			ProductCategory category = GetObject<ProductCategory>(categoryID);
			if (category != null) {
				return Templates.AlreadyExists(categoryID);
			}

			//Create category, languageitem
			LanguageItem item = new LanguageItem(categoryID + "_name", en, nl, ar);
			item.Upload(Connection);
			category = new ProductCategory(categoryID, item.Id);
			category.Upload(Connection);

			//Create response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};
		}
	}
}