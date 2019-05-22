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
		public static JObject deleteProductItem(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productItemID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productItemID");
			}

			string productID = idValue.ToString();
			if (productID == "0") {
				return Templates.InvalidArgument("productID");
			}

			//Check if productItem exists
			ProductItem item = Requests.getObject<ProductItem>(productID);
			if (item == null) {
				return Templates.NoSuchProduct;
			}
			item.Delete(wrapper);

			//Create base response
			return new JObject() {
				{"reason", null },
			};
		}
	}
}