using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject addProduct(JObject request)
		{
			//Get arguments
			string productID;
			string manufacturer;
			string categoryID;
			request.TryGetValue("productID", out JToken productIDValue);
			request.TryGetValue("categoryID", out JToken categoryIDValue);
			request.TryGetValue("manufacturer", out JToken manufacturerValue);
			request.TryGetValue("description", out JToken descriptionValue);
			request.TryGetValue("name", out JToken nameValue);

			// Verify presence of arguments
			List<string> failedVerifications = new List<string>();
			if (productIDValue == null) failedVerifications.Add("productID");
			if (categoryIDValue == null) failedVerifications.Add("categoryID");
			if (manufacturerValue == null) failedVerifications.Add("manufacturer");
			if (nameValue == null) failedVerifications.Add("name");

			if (failedVerifications.Any())
				return Templates.MissingArguments(failedVerifications.ToArray());

			// Verify arguments
			if (productIDValue.Type != JTokenType.String) failedVerifications.Add("productID");
			if (categoryIDValue.Type != JTokenType.String) failedVerifications.Add("categoryID");
			if (manufacturerValue.Type != JTokenType.String) failedVerifications.Add("manufacturer");
			if (nameValue.Type != JTokenType.Object) failedVerifications.Add("name");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Prepare values
			productID = productIDValue.ToObject<string>();
			manufacturer = manufacturerValue.ToObject<string>();
			categoryID = categoryIDValue.ToObject<string>();

			// Get image
			request.TryGetValue("image", out JToken imageValue);
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

			// Get languages
			string en;
			string nl = null;
			string ar = null;
			JObject names = nameValue.ToObject<JObject>();
			names.TryGetValue("en", out JToken nameEnValue);
			names.TryGetValue("nl", out JToken nameNlValue);
			names.TryGetValue("ar", out JToken nameArValue);
			if (nameEnValue != null && nameEnValue.Type == JTokenType.String) {
				en = names["en"].ToObject<string>();
			} else {
				return Templates.MissingArguments("name: en");
			}
			if (nameNlValue != null && nameNlValue.Type == JTokenType.String) {
				nl = names["nl"].ToObject<string>();
			}
			if (nameArValue != null && nameArValue.Type == JTokenType.String) {
				ar = names["ar"].ToObject<string>();
			}
			LanguageItem name = new LanguageItem(productID + "_name", en, nl, ar);

			LanguageItem description;
			if (descriptionValue != null && descriptionValue.Type == JTokenType.Object) {
				//Get description
				JObject desc = descriptionValue.ToObject<JObject>();
				desc.TryGetValue("en", out JToken descEnValue);
				desc.TryGetValue("nl", out JToken descNlValue);
				desc.TryGetValue("ar", out JToken descArValue);
				if (descEnValue != null && descEnValue.Type == JTokenType.String) {
					en = desc["en"].ToObject<string>();
				} else {
					return Templates.MissingArguments("description: en");
				}
				if (descNlValue != null && descNlValue.Type == JTokenType.String) {
					nl = desc["nl"].ToObject<string>();
				}
				if (descArValue != null && descArValue.Type == JTokenType.String) {
					ar = desc["ar"].ToObject<string>();
				}
				description = new LanguageItem(productID + "_description", en, nl, ar);
			} else {
				description = new LanguageItem(productID + "_description", null);
			}

			//Check if product already exists
			Product product = GetObject<Product>(productID);
			if (product != null) {
				return Templates.AlreadyExists(productID);
			}

			//Check if category exists
			ProductCategory category = GetObject<ProductCategory>(categoryID);
			if (category == null) {
				return Templates.NoSuchProductCategory(categoryID);
			}

			//Create product, languageItem, image
			name.Upload(Connection);
			description.Upload(Connection);
			if (imageData != null) {
				Image image = new Image(productID + "_image", imageData, extension);
				image.Upload(Connection);
				product = new Product(productID, manufacturer, categoryID, productID + "_name", productID + "_description", image.Id);
			} else {
				product = new Product(productID, manufacturer, categoryID, productID + "_name", productID + "_description");
			}
			product.Upload(Connection);

			//Create response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}