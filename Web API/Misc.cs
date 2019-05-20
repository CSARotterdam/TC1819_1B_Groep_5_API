using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace API
{
	class Misc {
		public static MySqlConditionBuilder CreateCondition(JObject criteria)
		{
			var condition = new MySqlConditionBuilder();
			int i = 0;
			foreach (KeyValuePair<string, JToken> pair in criteria)
			{
				if (i > 0) condition.And();
				condition.NewGroup();
				condition.Column(pair.Key);
				string value = (string)pair.Value;
				string[] operands = value.Split("OR");
				foreach (string operand in operands)
				{
					string[] split = operand.Split(" ");
					if (split[0] == "LIKE")
					{
						condition.Like(split[1]);
					}
					else
					{
						condition.Equals(operand, MySql.Data.MySqlClient.MySqlDbType.String);
					}
					if (operands.Last() != operand)
					{
						condition.Or();
					}
				}
				condition.ExitGroup();
				i++;
			}
			return condition;
		}
    }
}
