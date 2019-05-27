using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Collaborator)]
		public JObject getProductAvailability(JObject request) {
			//Get arguments
			request.TryGetValue("products", out JToken statTypeValue);
			if (statTypeValue == null || (statTypeValue.Type != JTokenType.String && statTypeValue.Type != JTokenType.Array)) {
				return Templates.MissingArguments("statType");
			}

			//Parse arguments
			List<string> productIDs = new List<string>();
			if (statTypeValue.Type == JTokenType.String) {
				//TODO Allow * to be used as value, selecting all products
				productIDs.Add(statTypeValue.ToObject<string>());
			} else if (statTypeValue.Type == JTokenType.Array) {
				productIDs = statTypeValue.ToObject<List<string>>();
			}

			//Create base response
			JObject response = new JObject();

			//Retrieve statistics
			foreach (string productID in productIDs) {
				//If the product doesn't exist, add an error entry
				Log.Debug($"{Connection.GetType().Name}@{Connection.GetHashCode().ToString("X")}");
				if (GetObject<Product>(productID) == null) {
					response[productID] = "NoSuchProduct";
					continue;
				}

				//Create base entry
				JObject entry = new JObject() {
					{"total", 0 },
					{"reservations", 0 },
					{"loanedOut", 0},
					{"inStock", 0}
				};

				//Get all loans belonging to this product
				List<ProductItem> productItems = Connection.Select<ProductItem>(new MySqlConditionBuilder()
					.Column("product")
					.Equals(productID, MySqlDbType.String)
				).ToList();
				List<LoanItem> loans = new List<LoanItem>();
				foreach (ProductItem pitem in productItems) {
					List<LoanItem> loanitems = Connection.Select<LoanItem>(new MySqlConditionBuilder()
						.Column("product_item")
						.Equals(pitem.Id, MySqlDbType.Int32)
					).ToList();
					loans.AddRange(loanitems);
				}

				//Count total productItems
				entry["total"] = productItems.Count();

				//Count total reservations
				entry["reservations"] = loans.Count;

				//Count total loanedOut
				foreach (LoanItem loan in loans) {
					if (loan.IsAcquired)
						entry["loanedOut"] = (int)entry["loanedOut"] + 1;
				}

				//Count total inStock
				entry["inStock"] = (int)entry["total"] - (int)entry["loanedOut"];

				//Add entry
				response[productID] = entry;
			}

			//Return response
			return new JObject() {
				{"reason", null },
				{"responseData", response}
			};
		}
	}
}