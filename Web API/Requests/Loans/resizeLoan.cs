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
		/// Handles requests with requestType "resizeLoan".
		/// </summary>
		/// <remarks>
		/// Required arguments are:
		///		- loanId > The id of the loan to edit.
		///		- start > Specifies a limit where all returned loans must have ended after this limit.
		///		- end > Specifies a limit where all returned loans must have started before this limit.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject resizeLoan(JObject request) {
			//Get arguments
			request.TryGetValue("loanId", out JToken loanItemID);
			request.TryGetValue("start", out JToken requestStart);
			request.TryGetValue("end", out JToken requestEnd);

			// Verify the arguments
			List<string> failedVerifications = new List<string>();
			if (loanItemID == null || loanItemID.Type != JTokenType.Integer)
				failedVerifications.Add("loanId");
			if (requestStart == null || !(requestStart.Type == JTokenType.String || requestStart.Type == JTokenType.Date))
				failedVerifications.Add("start");
			if (requestEnd == null || !(requestEnd.Type == JTokenType.String || requestEnd.Type == JTokenType.Date))
				failedVerifications.Add("end");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Parse arguments
			DateTime start;
			DateTime end;
			string loanID = loanItemID.ToString();
			try { start = DateTime.Parse(requestStart.ToString()); } catch (Exception) { return Templates.InvalidArgument("Unable to parse 'start'"); }
			try { end = DateTime.Parse(requestEnd.ToString()); } catch (Exception) { return Templates.InvalidArgument("Unable to parse 'end'"); }
			if (end < start) return Templates.InvalidArguments("'end' must come after 'start'");
			DateTimeSpan newLoanSpan = new DateTimeSpan(start, end);
			if (newLoanSpan.Start < DateTime.Now.Date) return Templates.InvalidArgument("'start' may not be set earlier than today.");
			if (newLoanSpan.Duration > MaxLoanDuration) return Templates.InvalidArgument($"Duration of the loan may not exceed {MaxLoanDuration.Days} days.");

			// Build a condition to get the specific loan
			var condition = new MySqlConditionBuilder();
			// Automatically limit results to current user only if their permission is User
			if (CurrentUser.Permission <= UserPermission.User)
				condition.And().Column("user").Equals(CurrentUser.Username, MySqlDbType.String);
			condition.And().Column("id").Equals(loanID, MySqlDbType.Int32);

			//Get the specified loanItem if it exists. If it doesn't, throw an error.
			LoanItem oldLoan = Connection.Select<LoanItem>(condition ).FirstOrDefault();
			if (oldLoan == null)
				return Templates.NoSuchLoan(loanID);

			// Return a loanExpired template if the loan has already ended
			if (oldLoan.End < DateTime.Now)
				return Templates.LoanExpired();
			if (oldLoan.Start < DateTime.Now && oldLoan.Start != start)
				return Templates.LoanAlreadyStarted();

			// Build condition
			condition = new MySqlConditionBuilder()
				.Column("product_item")
				.Equals(oldLoan.ProductItem, MySqlDbType.Int32);
			// Select only relevant loans
			condition.And().Column("end").GreaterThanOrEqual().Operand(start, MySqlDbType.DateTime);
			condition.And().Column("start").LessThanOrEqual().Operand(end, MySqlDbType.DateTime);

			//Get all loanItems for this loan's product_item.
			var loans = Connection.Select<LoanItem>(condition).ToList();

			//Check for conflicting loanItems
			bool canResize = true;
			foreach (var loan in loans)
			{
				var loanSpan = new DateTimeSpan(loan.Start, loan.End);
				if (newLoanSpan.Overlaps(loanSpan))
				{
					canResize = false;
					break;
				}
			}

			//Create response
			var responseData = new JObject();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			if (canResize)
			{
				// Update loan
				oldLoan.Start = start;
				oldLoan.End = end;
				Connection.Update(oldLoan);
			}
			else if (oldLoan.IsAcquired && !canResize) // If loan is not eligible for a item reassignment
				return Templates.LoanResizeFailed();
			else // If the loan is eligible for an item reassignment
			{
				// Try to retrieve a list of unreserved equivalent products
				var unreservedAlts = Core_GetUnreservedItems(oldLoan.GetProductItem(Connection).ProductId, newLoanSpan);
				if (unreservedAlts == null) // Normally this value should not be able to be null. This block is just a failsafe that adds context.
					throw new InvalidOperationException("Expected type of 'unreservedAlts' is List, but nullreference was found.");
				if (!unreservedAlts.Any())
				{
					Log.Error("Failed at line 107");
					return Templates.LoanResizeFailed();
				}
				// Update loan
				oldLoan.ProductItem = unreservedAlts.First().Id.Value;
				oldLoan.Start = start;
				oldLoan.End = end;
				Connection.Update(oldLoan);
				// Return new data in response and add message for context
				responseData["product_item"] = oldLoan.ProductItem;
				response["message"] = "Loan has been reassigned.";
			}

			return response;
		}
	}
}