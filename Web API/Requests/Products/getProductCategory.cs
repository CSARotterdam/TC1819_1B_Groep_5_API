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
		/// <summary>
		/// Handles requests with requestType "getProductCategory".
		/// </summary>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>

		[verifyPermission(User.UserPermission.User)]
		public static JObject getProductCategory(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("categoryID", out JToken idValue);
			requestData.TryGetValue("name", out JToken languageValue);
			if (idValue == null || idValue.Type != JTokenType.String || languageValue == null || languageValue.Type != JTokenType.Array) {
				return Templates.MissingArguments("productID, language");
			}

			string categoryID = idValue.ToString();

			//Create base response
			JObject response = new JObject() {
				{"reason", null },
				{"categoryData", null}
			};

			//Get product info
			ProductCategory category = Requests.getObject<ProductCategory>(categoryID);
			if (category == null) {
				response = Templates.NoSuchProductCategory;
				return response;
			}

			//Add product data to response
			response["categoryData"] = new JObject() {
				{"id",  category.Id},
				{"name", new JObject(){
					{ "en", null },
					{ "nl", null },
					{ "ar", null },
				}},
				{"image", null }
			};

			//Add product names, if any.
			LanguageItem item = category.GetName(wrapper);
			List<string> language = requestData["name"].ToObject<List<string>>();
			if (language.Contains("en")) {
				response["categoryData"]["name"]["en"] = item.en;
			}
			if (language.Contains("nl")) {
				response["categoryData"]["name"]["nl"] = item.nl;
			}
			if (language.Contains("ar")) {
				response["categoryData"]["name"]["ar"] = item.ar;
			}

			return response;
		}
	}
}