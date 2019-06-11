using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject addProductItem(JObject request) {
			//Get arguments
			string productID;
			int count;
			request.TryGetValue("productID", out JToken productIDValue);
			request.TryGetValue("count", out JToken countValue);
			if (productIDValue == null || productIDValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			} else {
				productID = productIDValue.ToObject<string>();
			}
			if(countValue == null || countValue.Type != JTokenType.Integer) {
				count = 1;
			} else {
				count = countValue.ToObject<int>();
				if(count > 30) {
					return Templates.InvalidArgument("count");
				}
			}

			//Check if product exists
			Product product = GetObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			//Create productItems
			List<int> IDs = new List<int>();
			for(int i = count; i != 0; i--) {
				ProductItem item = new ProductItem(null, productID);
				item.Upload(Connection);
				IDs.Add(item.Id.Value);
			}

			//Create response
			return new JObject() {
				{"reason", null },
				{"responseData", new JArray(IDs) }
			};
		}
	}
}