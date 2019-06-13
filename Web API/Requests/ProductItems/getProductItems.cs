using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests
{
	abstract partial class RequestHandler
	{
		/// <summary>
		/// Handles requests with requestType "getProductItems".
		/// </summary>
		/// <remarks>
		/// Optional arguments are:
		///		- products > a string[] specifying which product's items to return. If null or empty, this condition will be ignored.
		///		- itemIds > an int[] specifying which productItems to return. If null or empty, this condition will be ignored.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject getProductItems(JObject request)
		{
			// Get request arguments
			request.TryGetValue("products", out JToken requestProductIds);
			request.TryGetValue("itemIds", out JToken requestItemIds);

			// Verify the presence of at least one argument
			if (requestProductIds == null && requestItemIds == null)
				return Templates.MissingArguments("products", "itemIds");

			// Verify the argument
			if (requestProductIds != null && (requestProductIds.Type != JTokenType.Array || requestProductIds.Any(x => x.Type != JTokenType.String)))
				return Templates.InvalidArgument("products");
			if (requestItemIds != null && (requestItemIds.Type != JTokenType.Array || requestItemIds.Any(x => x.Type != JTokenType.Integer)))
				return Templates.InvalidArgument("itemIds");

			//Create base response
			var responseData = new JObject();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			// Prepare values
			requestProductIds = requestProductIds ?? new JArray();
			requestItemIds = requestItemIds ?? new JArray();

			// Request ProductItem data from database
			ILookup<string, ProductItem> productItemData = Core_getProductItems(requestProductIds.ToObject<string[]>(), requestItemIds.ToObject<int[]>());

			// Add all grouped productItems as dictionaries to responseData
			foreach (var data in productItemData)
			{
				// Creates an array with the key of the productId, containing all associated items
				var items = new JArray();
				foreach (var productItem in data)
					items.Add(productItem.Id);
				responseData[data.Key] = items;
			}

			return response;
		}

		/// <summary>
		/// Heart of the getProductItems function.
		/// </summary>
		/// <param name="productIds"></param>
		/// <returns></returns>
		private ILookup<string, ProductItem> Core_getProductItems(string[] productIds, int[] itemIds = null)
		{
			productIds = productIds ?? new string[0];
			itemIds = itemIds ?? new int[0];

			// Build condition
			MySqlConditionBuilder condition = new MySqlConditionBuilder("product", MySqlDbType.String, productIds);
			condition.And();
			condition.NewGroup();
			foreach (var id in itemIds)
			{
				condition.Or()
					.Column("id")
					.Equals(id, MySqlDbType.Int32);
			}
			condition.EndGroup();

			var itemPrimary = ProductItem.indexes.First(x => x.Type == Index.IndexType.PRIMARY).Columns[0];
			// Ignore default item
			condition.And()
				.Column(itemPrimary.Column)
				.NotEquals(0, itemPrimary.Type);

			return Connection.Select<ProductItem>(condition).ToLookup(x => x.ProductId);
		}
	}
}