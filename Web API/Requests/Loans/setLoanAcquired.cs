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
			request.TryGetValue("loanItemID", out JToken requestLoanItemId);
			request.TryGetValue("value", out JToken requestAcquiredValue);

			// Verify presence of arguments
			var failedVerifications = new List<string>();
			if (requestLoanItemId == null)
				failedVerifications.Add("loanItemID");
			if (requestAcquiredValue == null)
				failedVerifications.Add("value");

			if (failedVerifications.Any())
				return Templates.MissingArguments(failedVerifications.ToArray());

			// Verify arguments
			if (requestLoanItemId.Type != JTokenType.Integer)
				failedVerifications.Add("loanItemID");
			if (requestAcquiredValue.Type != JTokenType.Boolean)
				failedVerifications.Add("value");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			// Get loanItem
			var condition = new MySqlConditionBuilder("id", MySqlDbType.Int32, (object)requestLoanItemId.ToObject<int>());
			var loanItem = Connection.Select<LoanItem>(condition).FirstOrDefault();
			if (loanItem == null)
				return Templates.NoSuchLoan(requestLoanItemId.ToString());

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
