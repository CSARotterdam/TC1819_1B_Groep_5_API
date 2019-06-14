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
		/// Returns an array of dates within a given range where all items of a given product are reserved. This can be used
		/// to inform the user which days are not suitable for loans.
		/// </summary>
		/// <remarks>
		/// Required arguments are:
		///		- productId > The id of the product from which to find a set of loanItems.
		///		- start > The minimum date that will be checked for availability.
		///		- end > The maximum date that will be checked for availability. The range between start and end may not exceed 4 months.
		/// </remarks>
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject getUnavailableDates(JObject request)
		{
			// Get arguments
			request.TryGetValue("productId", out JToken requestProductId);
			request.TryGetValue("start", out JToken requestStart);
			request.TryGetValue("end", out JToken requestEnd);

			// Verify arguments
			List<string> failedVerifications = new List<string>();
			if (requestProductId == null || requestProductId.Type != JTokenType.String)
				return Templates.InvalidArgument("productId");
			if (requestStart == null)
				return Templates.InvalidArgument("start");
			if (requestEnd == null)
				return Templates.InvalidArgument("end");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Parse arguments
			DateTime start;
			DateTime end;
			try { start = requestStart == null ? new DateTime() : DateTime.Parse(requestStart.ToString()); }
			catch (Exception) { return Templates.InvalidArgument("Unable to parse 'start'"); }
			try { end = requestEnd == null ? new DateTime() : DateTime.Parse(requestEnd.ToString()); }
			catch (Exception) { return Templates.InvalidArgument("Unable to parse 'end'"); }
			var range = new DateTimeSpan(start, end);
			if (range.Duration.Days > 122)
				return Templates.InvalidArgument("start and end may not be more than 122 days apart.");

			// Get all items
			var condition = new MySqlConditionBuilder("product", MySqlDbType.String, requestProductId.ToString());
			var productItems = Connection.Select<ProductItem>(condition).ToArray();

			// Return empty response if no items were found
			if (!productItems.Any())
				return new JObject() {
					{"reason", null },
					{"responseData", new JArray() }
				};

			// Get all loans within the specified range
			condition = new MySqlConditionBuilder("product_item", MySqlDbType.Int32, productItems.Select(x => x.Id).Cast<object>().ToArray());
			condition.And()
				.Column("end")
				.GreaterThanOrEqual()
				.Operand(range.Start, MySqlDbType.DateTime);
			condition.And()
				.Column("start")
				.LessThanOrEqual()
				.Operand(range.End, MySqlDbType.DateTime);
			var loanItems = Connection.Select<LoanItem>(condition).ToArray();

			// No idea if this is the most efficient way, but it basically iterates
			// through all dates of a loan and slaps them in a cache. If the cache
			// already contains the date, it increases the counter.
			// If the counter is larger or equal to the total amount of items, we
			// consider that date unavailable.
			var dateCache = new Dictionary<DateTime, int>();
			foreach (var loan in loanItems)
			{
				while (loan.Start < loan.End)
				{
					if (loan.Start < range.Start)
					{
						loan.Start = loan.Start.AddDays(1);
						continue;
					}
					if (loan.End > range.End) break;
					if (dateCache.ContainsKey(loan.Start))
						dateCache[loan.Start]++;
					else
						dateCache[loan.Start] = 1;
					loan.Start = loan.Start.AddDays(1);
				}
			}

			// Build base response
			var responseData = new JArray();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};
			
			// Add all dates with an amount of loans greater or equal to the max available items
			foreach ((DateTime date, int count) in dateCache)
				if (count >= productItems.Length)
					responseData.Add((long)(date.ToUniversalTime() - Epoch).TotalMilliseconds);

			return response;
		}
	}
}
