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
		public static JObject updateProductItem(JObject request) {

			//Validate arguments
			string productItemID;
			string productID = null;


			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productItemID", out JToken itemIdValue);
			requestData.TryGetValue("productID", out JToken idValue);
			if (itemIdValue == null || itemIdValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productItemID");
			} else {
				productItemID = itemIdValue.ToObject<string>();
			}
			if (idValue != null || idValue.Type != JTokenType.String) {
				productID = idValue.ToObject<string>();
				if(productID == "0") {
					return Templates.InvalidArgument("productID");
				}
			}

			//Get product, if it exists
			Product product = Requests.getObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct;
			}

			//get productItem, if it exists
			ProductItem item = Requests.getObject<ProductItem>(productItemID);
			if (item == null) {
				return Templates.NoSuchProductItem;
			}

			//Change product ID, if necessary
			if(productID != null) {
				item.ProductId = productID;
			}

			item.Update(wrapper);

			//Create response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}