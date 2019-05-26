﻿using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {

		[verifyPermission(User.UserPermission.Admin)]
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

			//Get image
			requestData.TryGetValue("image", out JToken imageValue);
			string extension = null;
			byte[] imageData = null;
			if (imageValue != null && imageValue.Type == JTokenType.Object) {
				JObject image = imageValue.ToObject<JObject>();
				image.TryGetValue("data", out JToken dataValue);
				image.TryGetValue("extension", out JToken extensionValue);
				if (extensionValue != null && extensionValue.Type == JTokenType.String &&
					dataValue != null && dataValue.Type == JTokenType.String) {
					extension = extensionValue.ToObject<string>();
					imageData = (byte[])dataValue;
					if (!Image.ImageFormats.Contains(extension)) {
						return Templates.InvalidArgument("extension");
					}
				} else {
					return Templates.MissingArguments("data, extension");
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



			//Check if product already exists
			Product product = Requests.getObject<Product>(productID);
			if (product != null) {
				return Templates.AlreadyExists(productID);
			}

			//Check if category exists
			ProductCategory category = Requests.getObject<ProductCategory>(categoryID);
			if (category != null) {
				return Templates.NoSuchProductCategory(categoryID);
			}

			//Create product, languageItem, image
			LanguageItem item = new LanguageItem(productID + "_name", en, nl, ar);
			item.Upload(wrapper);
			if (imageData != null) {
				Image image = new Image(productID + "_image", imageData, extension);
				image.Upload(wrapper);
				product = new Product(productID, manufacturer, categoryID, productID + "_name", image.Id);
			} else {
				product = new Product(productID, manufacturer, categoryID, productID + "_name");
			}
			product.Upload(wrapper);

			//Create response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}