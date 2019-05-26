using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests
{
	static partial class RequestMethods
	{
		/// <summary>
		/// Handles requests with requestType "deleteLoan".
		/// </summary>
		/// <remarks>
		/// Optional arguments are:
		///		- Columns > a string[] specifying what fields to return. If omitted or empty, all fields will be returned.
		///		- Start > Specifies a limit where all returned loans must have ended after this limit.
		///		- End > Specifies a limit where all returned loans must have started before this limit.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[verifyPermission(User.UserPermission.User)]
		public static JObject deleteLoan(JObject request)
		{
			// Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("loanId", out JToken loanId);

			// Verify arguments
			if (loanId == null || loanId.Type != JTokenType.Integer)
				return Templates.InvalidArgument("loanId");
			
			// Build condition
			var condition = new MySqlConditionBuilder();
			if (CurrentUser.Permission <= User.UserPermission.User)
				condition.Column("user").Equals(CurrentUser.Username, MySqlDbType.String);
			condition.And().Column("id").Equals(loanId, MySqlDbType.String);
			
			// Get and delete loan, or return error if no such loan exists
			var loan = wrapper.Select<LoanItem>(condition, range: (0, 1)).FirstOrDefault();
			if (loan == null)
				return Templates.NoSuchLoan(loanId.ToString());
			if (loan.Start < DateTime.Now)
				return Templates.LoanAlreadyStarted();
			wrapper.Delete(loan);
			
			// Create response
			JObject response = new JObject() {
				{"reason", null }
			};
			return response;
		}
	}
}
