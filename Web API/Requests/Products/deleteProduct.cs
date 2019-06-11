﻿using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {

		[RequiresPermissionLevel(UserPermission.Admin)]
		public JObject deleteProduct(JObject request) {
			//Get arguments
			request.TryGetValue("productID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			}

			// Prepare values
			string productID = idValue.ToString();


			//Check if product exists
			Product product = GetObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct(productID);
			}

			// Check if items or acquired loans exist
			var condition = new MySqlConditionBuilder("product", MySqlDbType.String, productID);
			var items = Connection.Select<ProductItem>(condition).ToList();

			// Get associated loans
			condition = new MySqlConditionBuilder("product_item", MySqlDbType.Int32, items.Select(x => x.Id).Cast<object>().ToArray());
			List<bool> loans_isAcquired = Connection.Select<LoanItem>(new string[] { "is_item_acquired" }, condition).Select(x => (bool)x[0]).ToList();

			// Check if any loans are aquired
			if (loans_isAcquired.Any(x => x))
				return Templates.CannotDelete("This product still has active loans.");

			// Delete image
			product.Delete(Connection);
			if (product.Image != "default") {
				product.GetImage(Connection)?.Delete(Connection);
			}
			// Delete name languageItem if it isnt the default languageItem
			if (product.Name != "0") {
				product.GetName(Connection)?.Delete(Connection);
			}
			// Delete description languageItem if it isnt the default languageItem
			if (product.Description != "0") {
				product.GetDescription(Connection)?.Delete(Connection);
			}

			//Create base response
			return new JObject() {
				{"reason", null },
			};

		}
	}
}