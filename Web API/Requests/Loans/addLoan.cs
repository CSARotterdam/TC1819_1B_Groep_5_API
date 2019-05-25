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
		[verifyPermission(User.UserPermission.User)]
		public static JObject addLoan(JObject request)
		{

			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("productId", out JToken requestProductId);
			requestData.TryGetValue("start", out JToken requestStart);
			requestData.TryGetValue("end", out JToken requestEnd);

			// Verify the arguments
			List<string> failedVerifications = new List<string>();
			if (requestProductId == null || requestProductId.Type != JTokenType.String)
				failedVerifications.Add("productID");
			if (requestStart == null || requestStart.Type != JTokenType.String)
				failedVerifications.Add("start");
			if (requestEnd == null || requestEnd.Type != JTokenType.String)
				failedVerifications.Add("end");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Parse arguments
			DateTime start;
			DateTime end;
			string productId = requestProductId.ToString();
			try { start = DateTime.Parse(requestStart.ToString()); }
			catch (Exception) { return Templates.InvalidArgument("Unable to parse 'start'"); }
			try { end = DateTime.Parse(requestEnd.ToString()); }
			catch (Exception) { return Templates.InvalidArgument("Unable to parse 'end'"); }
			if (end < start) return Templates.InvalidArguments("'end' must come after 'start'");
			if (start < DateTime.Now.Date) return Templates.InvalidArgument("'start' may not be set earlier than today.");
			if (end - start > MaxLoanDuration) return Templates.InvalidArgument($"Duration of the loan may not exceed {MaxLoanDuration.Days} days.");

			ProductItem item;
			try {
				item = Core_GetUnreservedItem(productId, new DateTimeSpan(start, end));
			} catch (OperationCanceledException e) {
				return Templates.NoItemsForProduct(e.Message);
			}
			if (item == null)
				return Templates.ReservationFailed($"Product '{productId}' has no items available during this time.");

			var loan = new LoanItem(null, CurrentUser.Username, item.Id.Value, start, end);
			wrapper.Upload(loan);

			//Create response
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", new JObject() {
					{"loanId", loan.Id },
					{"productItem", item.Id }
				}}
			};

			return response;
		}

		private static ProductItem Core_GetUnreservedItem(string productId, DateTimeSpan span)
		{
			List<ProductItem> items = Core_getProductItems(productId)[productId].ToList();
			if (!items.Any()) throw new OperationCanceledException("No items are associated with this productId.");
			//TODO: returning null might be better ^^^^^^

			var condition = new MySqlConditionBuilder();
			condition.NewGroup();
			foreach (var item in items)
			{
				condition.Or()
					.Column("product_item")
					.Equals(item.Id, item.GetIndex("PRIMARY").Columns[0].Type);
			}
			condition.ExitGroup();
			condition.And()
				.Column("end")
				.GreaterThanOrEqual()
				.Operand(span.Start, MySqlDbType.DateTime);

			List<LoanItem> loans = wrapper.Select<LoanItem>(condition).ToList();
			foreach (var loan in loans)
			{
				if (!items.Any(x => x.Id == loan.ProductItem))
					continue;
				var loanSpan = new DateTimeSpan(loan.Start, loan.End);
				if (span.Overlaps(loanSpan)) items.Remove(items.First(x => x.Id == loan.ProductItem));
			}
			return items.FirstOrDefault();
		}

	}
}