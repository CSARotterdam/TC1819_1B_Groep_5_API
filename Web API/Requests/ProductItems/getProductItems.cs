using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests
{
	static partial class RequestMethods
	{
		/// <summary>
		/// Handles requests with requestType "getProductItems".
		/// </summary>
		/// <remarks>
		/// Optional arguments are:
		///		- products > a string[] specifying which product's items to return. If null or empty, all items will be returned.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[verifyPermission(User.UserPermission.User)]
		public static JObject getProductItems(JObject request)
		{
			// Get request arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("products", out JToken requestProductIds);

			// Verify the argument
			if (requestProductIds != null && (requestProductIds.Type != JTokenType.Array || requestProductIds.Any(x => x.Type != JTokenType.String)))
				return Templates.InvalidArgument("products");

			//Create base response
			var responseData = new JObject();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			// Prepare values
			requestProductIds = requestProductIds ?? new JArray();

			// Request ProductItem data from database
			ILookup<string, ProductItem> categoryData = Core_getProductItems(requestProductIds.ToObject<string[]>());

			// Add all categories as dictionaries to responseData
			foreach (var data in categoryData)
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
		private static ILookup<string, ProductItem> Core_getProductItems(params string[] productIds)
		{
			MySqlConditionBuilder condition = new MySqlConditionBuilder();

			// Build condition
			foreach (var id in productIds)
			{
				condition.Or();
				condition.Column("product");
				condition.Equals(id, MySqlDbType.String);
			}
			var itemPrimary = ProductItem.indexes.First(x => x.Type == Index.IndexType.PRIMARY).Columns[0];
			// Ignore default item
			condition.And()
				.Column(itemPrimary.Column)
				.NotEquals(0, itemPrimary.Type);

			return wrapper.Select<ProductItem>(condition).ToLookup(x => x.ProductId);
		}
	}
}