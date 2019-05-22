using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {

		[verifyPermission(User.UserPermission.Collaborator)]
		public static JObject updateProductCategory(JObject request) {

			//Validate arguments
			string categoryID;
			string newCategoryID = null;
			JObject names = null;

			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("categoryID", out JToken categoryIDValue);
			requestData.TryGetValue("newCategoryID", out JToken newCategoryIDValue);
			requestData.TryGetValue("name", out JToken nameValue);
			if (categoryIDValue == null || categoryIDValue.Type != JTokenType.String) {
				return Templates.MissingArguments("categoryID");
			} else {
				categoryID = categoryIDValue.ToObject<string>();
				if(categoryID == "default" || categoryID == "uncategorized") {
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
			ProductCategory category = Requests.getObject<ProductCategory>(categoryID);
			if (category == null) {
				return Templates.NoSuchProductCategory(categoryID);
			}

			///////////////LanguageItem
			//Edit the LanguageItem if needed;
			LanguageItem item = category.GetName(wrapper);
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
				item.Update(wrapper);
			}

			//If a new product ID was specified, check if it already exists. If it doesn't, change the product ID.
			if (newCategoryID != null) {
				ProductCategory newProduct = Requests.getObject<ProductCategory>(newCategoryID);
				if (newProduct != null) {
					return Templates.AlreadyExists(categoryID);
				} else {
					item.Id = newCategoryID + "_name";
					item.Update(wrapper);
					category.Name = item.Id;
					category.UpdateTrace();
					category.Id = newCategoryID;
				}
			}

			category.Update(wrapper);

			//Create response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};
		}
	}
}