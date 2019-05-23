﻿using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {

		[verifyPermission(User.UserPermission.Collaborator)]
		public static JObject deleteProduct(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			}

			string productID = idValue.ToString();

			//Check if product exists
			Product product = Requests.getObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			product.Delete(wrapper);
			Image image = product.GetImage(wrapper);
			if (image.Id != "default") {
				image.Delete(wrapper);
			}
			LanguageItem name = product.GetName(wrapper);
			if (name.Id != "0") {
				name.Delete(wrapper);
			}

			//Create base response
			return new JObject() {
				{"reason", null },
			};

		}
	}
}