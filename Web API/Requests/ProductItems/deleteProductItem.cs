using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject deleteProductItem(JObject request) {
			//Get arguments
			request.TryGetValue("productItemID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productItemID");
			}

			string productID = idValue.ToString();
			if (productID == "0") {
				return Templates.InvalidArgument("productID");
			}

			//Check if productItem exists
			ProductItem item = GetObject<ProductItem>(productID);
			if (item == null) {
				return Templates.NoSuchProduct(productID);
			}
			item.Delete(Connection);

			//Create base response
			return new JObject() {
				{"reason", null },
			};
		}

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject deleteProductItems(JObject request)
		{
			//Get arguments
			request.TryGetValue("productId", out JToken requestProductId);
			request.TryGetValue("count", out JToken requestCount);

			// Verify presence of arguments
			var failedVerifications = new List<string>();
			if (requestProductId == null)
				failedVerifications.Add("productId");
			if (requestCount == null)
				failedVerifications.Add("count");

			if (failedVerifications.Any())
				return Templates.MissingArguments(failedVerifications.ToArray());

			// Verify arguments
			if (requestProductId.Type != JTokenType.String)
				failedVerifications.Add("productId");
			if (requestCount.Type != JTokenType.Integer || ((int)requestCount) < 0)
				failedVerifications.Add("count");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Get all productItems
			var condition = new MySqlConditionBuilder("product", MySqlDbType.String, requestProductId.ToString());
			var productItems = Connection.Select<ProductItem>(condition).ToArray();

			// Get all aquired loans that have yet to end
			condition = new MySqlConditionBuilder("product_item", MySqlDbType.Int32, productItems.Select(x => x.Id).Cast<object>().ToArray());
			condition.And()
				.Column("end")
				.GreaterThanOrEqual()
				.Operand(DateTime.Now, MySqlDbType.DateTime);
			condition.And()
				.Column("is_item_acquired")
				.Equals().True();
			var loans = Connection.Select<LoanItem>(condition).ToArray();

			// Filter items that aren't loaned out
			var deletableItems = productItems.Where(x => loans.FirstOrDefault(y => y.ProductItem == x.Id) == null).Take((int)requestCount).ToArray();

			// Delete all items
			foreach (var item in deletableItems)
				Connection.Delete(item);

			//Create base response
			return new JObject() {
				{"reason", null },
				{"responseData", new JObject() {
					{"deleted", deletableItems.Length },
					{"ignored", productItems.Length - deletableItems.Length }
				}}
			};
		}
	}
}