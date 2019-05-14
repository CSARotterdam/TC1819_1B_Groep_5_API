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
		public static JObject addProduct(JObject request) {
			//Get arguments
			string productID;
			string manufacturer;
			string categoryID;
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productID", out JToken productIDValue);
			requestData.TryGetValue("categoryID", out JToken categoryIDValue);
			requestData.TryGetValue("manufacturer", out JToken manufacturerValue);
			requestData.TryGetValue("name", out JToken nameValue);
			if (productIDValue == null || productIDValue.Type != JTokenType.String ||
				manufacturerValue == null || manufacturerValue.Type != JTokenType.String ||
				nameValue == null || nameValue.Type != JTokenType.Object ||
				categoryIDValue == null || categoryIDValue.Type != JTokenType.String
			) {
				return Templates.MissingArguments("productID, categoryID, manufacturer, name");
			} else {
				productID = productIDValue.ToObject<string>();
				manufacturer = manufacturerValue.ToObject<string>();
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
			if (enValue == null || enValue.Type != JTokenType.String) {
				return Templates.MissingArguments("en");
			} else {
				en = names["en"].ToObject<string>();
			}
			if(nlValue != null && nlValue.Type == JTokenType.String) {
				nl = names["nl"].ToObject<string>();
			}
			if(arValue != null && arValue.Type == JTokenType.String) {
				ar = names["ar"].ToObject<string>();
			}

			//Check if product already exists
			Product product = Requests.getProduct(productID);
			if(product != null) {
				return Templates.AlreadyExists;
			}

			//Check if category exists
			ProductCategory category = Requests.getProductCategory(productID);
			if (category != null) {
				return Templates.NoSuchProductCategory;
			}

			//Create product + languageItem
			LanguageItem item = new LanguageItem(productID+"_name", en, nl, ar);
			item.Upload(wrapper);
			product = new Product(productID, manufacturer, categoryID, productID + "_name");
			product.Upload(wrapper);

            //Create response
            return new JObject() {
                {"reason", null },
                {"success", true}
            };
        }
    }
}