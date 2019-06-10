using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler
	{
		/// <summary>
		/// Handles requests with requestType "getLoans".
		/// </summary>
		/// <remarks>
		/// Optional arguments are:
		///		- columns > a string[] specifying what fields to return. If omitted or empty, all fields will be returned.
		///		- productItemIds > a string[] the that all returned loans must be about any of these items.
		///		- userId > The user whose loans to return. Only works for Collaborators or higher.
		///		- start > Specifies a limit where all returned loans must have ended after this limit.
		///		- end > Specifies a limit where all returned loans must have started before this limit.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject getLoans(JObject request)
		{
			// Get arguments
			request.TryGetValue("columns", out JToken requestColumns);
			request.TryGetValue("productItemIds", out JToken requestProductItems);
			request.TryGetValue("userId", out JToken requestUserId);
			request.TryGetValue("loanItemID", out JToken requestLoanId);
			request.TryGetValue("start", out JToken requestStart);
			request.TryGetValue("end", out JToken requestEnd);

			// Verify arguments
			List<string> failedVerifications = new List<string>();
			if (requestColumns != null && (requestColumns.Type != JTokenType.Array || requestColumns.Any(x => x.Type != JTokenType.String)))
				return Templates.InvalidArgument("columns");
			if (requestProductItems != null && (requestProductItems.Type != JTokenType.Array || requestProductItems.Any(x => x.Type != JTokenType.String)))
				return Templates.InvalidArgument("productItemIds");
			if (requestUserId != null && requestUserId.Type != JTokenType.String)
				return Templates.InvalidArgument("userId");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Parse arguments
			DateTime start;
			DateTime end;
			try { start = requestStart == null ? new DateTime() : DateTime.Parse(requestStart.ToString()); }
			catch (Exception) { return Templates.InvalidArgument("Unable to parse 'start'"); }
			try { end = requestEnd == null ? new DateTime() : DateTime.Parse(requestEnd.ToString()); }
			catch (Exception) { return Templates.InvalidArgument("Unable to parse 'end'"); }

			// Prepare values
			if (requestColumns == null || requestColumns.Count() == 0)
				requestColumns = new JArray(LoanItem.metadata.Select(x => x.Column));

			// Build condition
			var condition = new MySqlConditionBuilder();
			if (requestProductItems != null && requestProductItems.Any())
			{
				condition.NewGroup();
				foreach (var productItem in requestProductItems)
					condition.Or()
						.Column("product_item")
						.Equals(productItem, MySqlDbType.String);
				condition.EndGroup();
			}
			// Filter by specific loan id
			if (requestLoanId != null)
				condition.And().Column("id").Equals(requestLoanId, MySqlDbType.Int32);
			// Filter by user
			if (requestUserId != null)
				condition.And().Column("user").Equals(requestUserId, MySqlDbType.String);
			// Automatically limit results to current user only if their permission is User
			if (CurrentUser.Permission <= UserPermission.User)
				condition.And().Column("user").Equals(CurrentUser.Username, MySqlDbType.String);
			// Select only relevant loans
			if (requestStart != null)
				condition.And().Column("end").GreaterThanOrEqual().Operand(start, MySqlDbType.DateTime);
			if (requestEnd != null)
				condition.And().Column("start").LessThanOrEqual().Operand(end, MySqlDbType.DateTime);

			// Get loans
			var loans = Connection.Select<LoanItem>(requestColumns.ToObject<string[]>(), condition);

			// Build base response
			var responseData = new JArray();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			// Populate responseData
			foreach (var loanData in loans)
			{
				var item = new JObject();
				for (int i = 0; i < requestColumns.Count(); i++)
				{
					if (loanData[i] is DateTime)
						item[(string)requestColumns[i]] = new JValue(((DateTime)loanData[i]).ToUniversalTime().Subtract(Epoch).TotalMilliseconds);
					else
						item[(string)requestColumns[i]] = new JValue(loanData[i]);
				}
				responseData.Add(item);
			}

			return response;
		}
	}
}
