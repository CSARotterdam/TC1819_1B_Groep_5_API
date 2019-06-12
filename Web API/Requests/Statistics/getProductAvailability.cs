using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.User)]
		public JObject getProductAvailability(JObject request) {
			//Get arguments
			request.TryGetValue("products", out JToken productValue);
			if (productValue == null || (productValue.Type != JTokenType.String && productValue.Type != JTokenType.Array)) {
				return Templates.MissingArguments("statType");
			}

			//Parse arguments
			List<string> productIDs = new List<string>();
			if (productValue.Type == JTokenType.String) {
				//TODO Allow * to be used as value, selecting all products
				productIDs.Add(productValue.ToObject<string>());
			} else if (productValue.Type == JTokenType.Array) {
				productIDs = productValue.ToObject<List<string>>();
			}

			// Build condition that matches all relevant productItems and get said productItems
			// The lookup groups the productItems and loanItems to a single product id.
			var condition = new MySqlConditionBuilder("product", MySqlDbType.String, productIDs.ToArray());
			var productItemsArray = Connection.Select<ProductItem>(condition).ToArray();
			var productItems = productItemsArray.ToLookup(x => x.ProductId);

			// Build condition that matches all relevand loanItems and get said loanItems
			condition = new MySqlConditionBuilder("product_item", MySqlDbType.Int32, productItemsArray.Select(x => x.Id.Value).Cast<object>().ToArray());
			var loanItems = Connection.Select<LoanItem>(condition).ToLookup(x => productItemsArray.First(y => y.Id == x.ProductItem).ProductId);

			// DateTimeSpan representing now->midnight for filtering relevant loans
			DateTimeSpan today = new DateTimeSpan(DateTime.Now, DateTime.Now.Date.AddDays(1));

			// Build response data
			var responseData = new JObject();
			foreach (var productID in productIDs)
			{
				var items = productItems[productID];
				var loans = loanItems[productID];
				var relevantLoans = loans.Where(x => today.Overlaps(x.Start, x.End)).ToArray();

				var entry = new JObject() {
					{"total", items.Count() },
					{"totalReservations", loans.Count() },
					{"reservations", relevantLoans.Length },
					{"loanedOut", relevantLoans.Count(x => x.IsAcquired) },
					{"inStock", items.Count() - relevantLoans.Count(x => x.IsAcquired) },
					{"available", items.Count() - relevantLoans.Length }
				};

				responseData.Add(productID, entry);
			}

			//Return response
			return new JObject() {
				{"reason", null },
				{"responseData", responseData}
			};
		}
	}
}