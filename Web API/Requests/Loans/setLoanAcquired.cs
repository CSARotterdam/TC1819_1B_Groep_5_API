using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests
{
	abstract partial class RequestHandler
	{
		[RequiresPermissionLevel(UserPermission.Collaborator)]
		public JObject setLoanAcquired(JObject request)
		{
			// Get arguments
			request.TryGetValue("loanId", out JToken requestLoanId);
			request.TryGetValue("value", out JToken requestAcquiredValue);

			// Verify presence of arguments
			var failedVerifications = new List<string>();
			if (requestLoanId == null)
				failedVerifications.Add("loanId");
			if (requestAcquiredValue == null)
				failedVerifications.Add("value");

			if (failedVerifications.Any())
				return Templates.MissingArguments(failedVerifications.ToArray());

			// Verify arguments
			if (requestLoanId.Type != JTokenType.Integer)
				failedVerifications.Add("loanId");
			if (requestAcquiredValue.Type != JTokenType.Boolean)
				failedVerifications.Add("value");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Get loanItem
			var condition = new MySqlConditionBuilder("id", MySqlDbType.Int32, (object)requestLoanId.ToObject<int>());
			var loanItem = Connection.Select<LoanItem>(condition).FirstOrDefault();
			if (loanItem == null)
				return Templates.NoSuchLoan(requestLoanId.ToString());

			// Update the IsAcquired value if it isn't equal to the 'value' argument
			if ((bool)requestAcquiredValue != loanItem.IsAcquired)
			{
				loanItem.IsAcquired = (bool)requestAcquiredValue;
				Connection.Update(loanItem);
			}

			//Create base response
			return new JObject() {
				{"reason", null }
			};
		}
	}
}
