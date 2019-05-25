using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {
		[verifyPermission(User.UserPermission.User)]
		public static JObject extendLoan(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("loanID", out JToken loanItemID);
			requestData.TryGetValue("start", out JToken requestStart);
			requestData.TryGetValue("end", out JToken requestEnd);

			// Verify the arguments
			List<string> failedVerifications = new List<string>();
			if (loanItemID == null || loanItemID.Type != JTokenType.String)
				failedVerifications.Add("loanID");
			if (requestStart == null || requestStart.Type != JTokenType.String)
				failedVerifications.Add("start");
			if (requestEnd == null || requestEnd.Type != JTokenType.String)
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
			if (start < DateTime.Now.Date) return Templates.InvalidArgument("'start' may not be set earlier than today.");
			if (end - start > MaxLoanDuration) return Templates.InvalidArgument($"Duration of the loan may not exceed {MaxLoanDuration.Days} days.");

			//Get the specified loanItem if it exists. If it doesn't, throw an error.
			LoanItem loan = Requests.getObject<LoanItem>(loanID);
			if (loan == null) {
				return Templates.NoSuchLoan(loanID);
			}

			//Create new datetimespan
			DateTimeSpan range = new DateTimeSpan(start, end);

			//Get all loanItems for this loan's product_item.
			List<LoanItem> items = wrapper.Select<LoanItem>(new MySqlConditionBuilder()
				.Column("product_item")
				.Equals(loan.ProductItem, MySqlDbType.Int32)
			).ToList();

			//Check for conflicting loanItems
			bool success = true;
			List<LoanItem> overlapping = new List<LoanItem>();
			foreach(LoanItem item in items) {
				if(range.Overlaps(new DateTimeSpan(item.Start, item.End))) {
					success = false;
					overlapping.Add(item);
				}
			}

			//If conflicts were found, try to resolve them
			if (!success) {

				
			}

			//Update loan
			loan.Start = start;
			loan.End = end;
			loan.Update(wrapper);

			//Create response
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", new JObject() }
			};

			return response;
		}
	}
}