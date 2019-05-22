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
		public static JObject addProductItem(JObject request) {
			//Get arguments
			string productID;
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productID", out JToken productIDValue);
			if (productIDValue == null || productIDValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID, categoryID, manufacturer, name");
			} else {
				productID = productIDValue.ToObject<string>();
			}

			//Check if product exists
			Product product = Requests.getObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			//Create productItem
			ProductItem item = new ProductItem(null, productID);
			item.Upload(wrapper);
			
			//Create response
			return new JObject() {
				{"reason", null },
				{"productItemID",  item.Id}
			};
		}
	}
}