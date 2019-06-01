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

			//Get all relevant loans
			MySqlConditionBuilder query = new MySqlConditionBuilder();
			foreach (string ID in productIDs) {
				query.Or();
				query.Column("id");
				query.Equals(ID, MySqlDbType.String);
			}
			List<ProductItem> productItems = Connection.Select<ProductItem>(query).ToList();
			query = new MySqlConditionBuilder();
			foreach (ProductItem pitem in productItems) {
				query.Or();
				query.Column("id");
				query.Equals(pitem.Id, MySqlDbType.String);
			}
			List<LoanItem> loanItems = Connection.Select<LoanItem>(query).ToList();

			//Create response
			JObject results = new JObject();
			foreach (string productID in productIDs) {

				//Create base entry
				JObject entry = new JObject() {
					{"total", 0 },
					{"reservations", 0 },
					{"loanedOut", 0},
					{"inStock", 0}
				};

				//Calculate stats
				foreach(ProductItem productitem in productItems) {
					if(productitem.ProductId != productID) {
						continue;
					}
					entry["total"] = (int)entry["total"] + 1;

					foreach(LoanItem loan in loanItems) {
						if(loan.ProductItem != productitem.Id) {
							continue;
						}
						entry["reservations"] = (int)entry["reservations"] + 1;

						if (loan.IsAcquired) {
							entry["loanedOut"] = (int)entry["loanedOut"] + 1;
						}
					}
				}
				entry["inStock"] = (int)entry["total"] - (int)entry["loanedOut"];

				//Add entry
				results[productID] = entry;
			}

			//Return response
			return new JObject() {
				{"reason", null },
				{"responseData", results}
			};
		}
	}
}