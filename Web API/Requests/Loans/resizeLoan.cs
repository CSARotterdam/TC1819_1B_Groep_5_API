using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods
	{
		/// <summary>
		/// Handles requests with requestType "resizeLoan".
		/// </summary>
		/// <remarks>
		/// Optional arguments are:
		///		- loanId > The id of the loan to edit.
		///		- Start > Specifies a limit where all returned loans must have ended after this limit.
		///		- End > Specifies a limit where all returned loans must have started before this limit.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[verifyPermission(User.UserPermission.User)]
		public static JObject resizeLoan(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("loanId", out JToken loanItemID);
			requestData.TryGetValue("start", out JToken requestStart);
			requestData.TryGetValue("end", out JToken requestEnd);

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

			//Get the specified loanItem if it exists. If it doesn't, throw an error.
			var condition = new MySqlConditionBuilder();

			// Automatically limit results to current user only if their permission is User
			if (CurrentUser.Permission <= User.UserPermission.User)
				condition.And().Column("user").Equals(CurrentUser.Username, MySqlDbType.String);
			condition.And().Column("id").Equals(loanID, MySqlDbType.Int32);

			LoanItem oldLoan = wrapper.Select<LoanItem>(condition ).FirstOrDefault();
			if (oldLoan == null)
				return Templates.NoSuchLoan(loanID);

			// Build condition
			condition = new MySqlConditionBuilder()
				.Column("product_item")
				.Equals(oldLoan.ProductItem, MySqlDbType.Int32);
			// Select only relevant loans
			condition.And().Column("end").GreaterThanOrEqual().Operand(start, MySqlDbType.DateTime);
			condition.And().Column("start").LessThanOrEqual().Operand(end, MySqlDbType.DateTime);

			//Get all loanItems for this loan's product_item.
			var loans = wrapper.Select<LoanItem>(condition).ToList();

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
				wrapper.Update(oldLoan);
			}
			else if (oldLoan.IsAcquired && !canResize) // If loan is not eligible for a item reassignment
				return Templates.LoanResizeFailed();
			else // If the loan is eligible for an item reassignment
			{
				// Try to retrieve a list of unreserved equivalent products
				var unreservedAlts = Core_GetUnreservedItems(oldLoan.GetProductItem(wrapper).ProductId, newLoanSpan);
				if (unreservedAlts == null) // Normally this value should not be able to be null. This block is just a failsafe that adds context.
					throw new InvalidOperationException("Expected type of 'unreservedAlts' is List, but nullreference was found.");
				if (!unreservedAlts.Any())
				{
					log.Error("Failed at line 107");
					return Templates.LoanResizeFailed();
				}
				// Update loan
				oldLoan.ProductItem = unreservedAlts.First().Id.Value;
				oldLoan.Start = start;
				oldLoan.End = end;
				wrapper.Update(oldLoan);
				// Return new data in response and add message for context
				responseData["product_item"] = oldLoan.ProductItem;
				response["message"] = "Loan has been reassigned.";
			}

			return response;
		}

		public static void test()
		{
			var d1 = new DateTimeSpan(DateTime.Parse("2019-06-15 00:00:00"), DateTime.Parse("2019-06-17 00:00:00"));
			Console.WriteLine(d1.Duration);
			var d2 = new DateTimeSpan(DateTime.Parse("2019-05-30 00:00:00"), DateTime.Parse("2019-06-18 00:00:00"));
			Console.WriteLine(d2.Duration);
			Console.WriteLine(d1.Overlaps(d2));
			d2.End = d2.End.Subtract(new TimeSpan(24, 0, 0));
			Console.WriteLine(d2.End);
			Console.WriteLine(d2.Duration);
			Console.WriteLine(d1.Overlaps(d2));
			Console.WriteLine(d2.Overlaps(d1));
		}
	}
}