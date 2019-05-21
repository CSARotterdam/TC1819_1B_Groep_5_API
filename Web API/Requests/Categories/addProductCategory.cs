using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {

		[verifyPermission(User.UserPermission.Collaborator)]
		public static JObject addProductCategory(JObject request) {
			//Get arguments
			string categoryID;
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("categoryID", out JToken categoryIDValue);
			requestData.TryGetValue("name", out JToken nameValue);
			if (categoryIDValue == null || categoryIDValue.Type != JTokenType.String ||
				nameValue == null || nameValue.Type != JTokenType.Object
			) {
				return Templates.MissingArguments("categoryID, name");
			} else {
				categoryID = categoryIDValue.ToObject<string>();
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
			ProductCategory category = Requests.getObject<ProductCategory>(categoryID);
			if (category != null) {
				return Templates.AlreadyExists;
			}

			//Create category, languageitem
			LanguageItem item = new LanguageItem(categoryID + "_name", en, nl, ar);
			item.Upload(wrapper);
			category = new ProductCategory(categoryID, item.Id);
			category.Upload(wrapper);

			//Create response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};
		}
	}
}