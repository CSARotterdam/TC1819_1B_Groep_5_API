﻿using MySQLWrapper.Data;
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
		public static JObject updateProduct(JObject request) {

			//Get arguments
			string productID;
			string newProductID = null;
			string categoryID = null;
			string manufacturer = null;
			JObject names = null;

			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productID", out JToken idValue);
			requestData.TryGetValue("newProductID", out JToken newIDValue);
			requestData.TryGetValue("categoryID", out JToken categoryIDValue);
			requestData.TryGetValue("manufacturer", out JToken manufacturerValue);
			requestData.TryGetValue("name", out JToken nameValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			} else {
				productID = idValue.ToObject<string>();
			}
			if (newIDValue != null && newIDValue.Type == JTokenType.String) {
				newProductID = newIDValue.ToObject<string>();
			}
			if (categoryIDValue != null && categoryIDValue.Type == JTokenType.String) {
				categoryID = categoryIDValue.ToObject<string>();
			}
			if (manufacturerValue != null && manufacturerValue.Type == JTokenType.String) {
				manufacturer = manufacturerValue.ToObject<string>();
			}
			if (nameValue != null && nameValue.Type == JTokenType.Object) {
				names = nameValue.ToObject<JObject>();
			}

			//Get product, if it exists
			Product product = Requests.getProduct(productID);
			if (product == null) {
				return Templates.NoSuchProduct;
			}

			//Edit the LanguageItem if needed;
			LanguageItem item = product.GetName(wrapper);
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
			if (newProductID != null) {
				Product newProduct = Requests.getProduct(newProductID);
				if (newProduct != null) {
					return Templates.AlreadyExists;
				} else {
					item.Id = newProductID + "_name";
					item.Update(wrapper);
					product.Name = item.Id;
					product.UpdateTrace();
					product.Id = newProductID;
				}
			}

			//If a new category was specified, check if it exists. If it does, change the product category
			if (categoryID != null) {
				ProductCategory category = Requests.getProductCategory(categoryID);
				if (category == null) {
					return Templates.NoSuchProductCategory;
				} else {
					product.Category = categoryID;
				}
			}

			//If a new manufacturer was specified, change it.
			if (manufacturer != null) {
				product.Manufacturer = manufacturer;
			}

			product.Update(wrapper);

			//Create response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};
		}
    }
}