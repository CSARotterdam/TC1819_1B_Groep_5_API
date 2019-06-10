using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject updateProduct(JObject request) {

			//Validate arguments
			string productID;
			string newProductID = null;
			string categoryID = null;
			string manufacturer = null;
			string extension = null;
			byte[] imageData = null;
			JObject names = null;
			JObject descriptions = null;
			JObject newImage = null;

			request.TryGetValue("productID", out JToken idValue);
			request.TryGetValue("newProductID", out JToken newIDValue);
			request.TryGetValue("categoryID", out JToken categoryIDValue);
			request.TryGetValue("manufacturer", out JToken manufacturerValue);
			request.TryGetValue("name", out JToken nameValue);
			request.TryGetValue("description", out JToken descriptionValue);
			request.TryGetValue("image", out JToken imageValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			} else {
				productID = idValue.ToObject<string>();
				if (productID == "default") {
					return Templates.InvalidArgument("categoryID");
				}
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
			if(descriptionValue != null && descriptionValue.Type == JTokenType.Object) {
				descriptions = descriptionValue.ToObject<JObject>();
			}
			if (imageValue != null && imageValue.Type == JTokenType.Object) {
				newImage = imageValue.ToObject<JObject>();
				newImage.TryGetValue("data", out JToken dataValue);
				newImage.TryGetValue("extension", out JToken extensionValue);
				if (extensionValue != null && extensionValue.Type == JTokenType.String) {
					extension = extensionValue.ToObject<string>();
					if (!Image.ImageFormats.Contains(extension)) {
						return Templates.InvalidArgument("extension");
					}
				}
				if (dataValue != null && dataValue.Type == JTokenType.String) {
					imageData = (byte[])dataValue;
				}
			}

			//Get product, if it exists
			Product product = GetObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}
			//If a new ID was given, check if it exists first.
			Product newProduct = GetObject<Product>(newProductID);
			if (newProduct != null) {
				return Templates.AlreadyExists(productID);
			}

			///////////////Image
			//Edit image if needed;
			Image image = product.GetImage(Connection);
			if (newImage != null) {

				string oldID = image.Id;
				if (image.Id == "default") {
					image = new Image(product.Id + "_image", image.Data, image.Extension);
				}
				if (extension != null) {
					image.Extension = extension;
				}
				if (imageData != null) {
					image.Data = imageData;
				}

				if (oldID != image.Id) {
					image.Upload(Connection);
					product.UpdateTrace();
					product.Image = image.Id;
					product.Update(Connection);
				} else {
					image.Update(Connection);
				}
			}

			///////////////Name
			//Edit the LanguageItem if needed;
			LanguageItem name = product.GetName(Connection);
			if (names != null) {
				if (names.TryGetValue("en", out JToken enValue)) {
					if (enValue.Type == JTokenType.String) {
						name.en = enValue.ToObject<string>();
					}
				}
				if (names.TryGetValue("nl", out JToken nlValue)) {
					if (nlValue.Type == JTokenType.String) {
						name.nl = nlValue.ToObject<string>();
					}
				}
				if (names.TryGetValue("ar", out JToken arValue)) {
					if (arValue.Type == JTokenType.String) {
						name.ar = arValue.ToObject<string>();
					}
				}
				name.Update(Connection);
			}

			///////////////Description
			//Edit the LanguageItem if needed;
			LanguageItem description = product.GetDescription(Connection);
			if (descriptions != null) {
				if (descriptions.TryGetValue("en", out JToken enValue)) {
					if (enValue.Type == JTokenType.String) {
						description.en = enValue.ToObject<string>();
					}
				}
				if (descriptions.TryGetValue("nl", out JToken nlValue)) {
					if (nlValue.Type == JTokenType.String) {
						description.nl = nlValue.ToObject<string>();
					}
				}
				if (descriptions.TryGetValue("ar", out JToken arValue)) {
					if (arValue.Type == JTokenType.String) {
						description.ar = arValue.ToObject<string>();
					}
				}
				description.Update(Connection);
			}

			//If a new ID was specified, change the product ID.
			if (newProductID != null) {
				image.Id = newProductID + "_image";
				image.Update(Connection);
				product.Image = image.Id;
				name.Id = newProductID + "_name";
				name.Update(Connection);
				description.Id = newProductID + "_description";
				description.Update(Connection);
				product.Name = name.Id;
				product.UpdateTrace();
				product.Id = newProductID;
			}

			///////////////Product
			//If a new category was specified, check if it exists. If it does, change the product category
			if (categoryID != null) {
				ProductCategory category = GetObject<ProductCategory>(categoryID);
				if (category == null) {
					return Templates.NoSuchProductCategory(categoryID);
				} else {
					product.Category = categoryID;
				}
			}

			//If a new manufacturer was specified, change it.
			if (manufacturer != null) {
				product.Manufacturer = manufacturer;
			}

			product.Update(Connection);

			//Create response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};
		}
	}
}