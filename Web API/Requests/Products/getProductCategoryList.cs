using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	static partial class RequestMethods {

		[verifyPermission(User.UserPermission.User)]
		public static JObject getProductCategoryList(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("criteria", out JToken criteriaValue);
			if (criteriaValue.Type != JTokenType.Object) {
				return Templates.MissingArguments("criteria");
			}

			//Parse criteria and use them to build a query;
			MySqlConditionBuilder query = new MySqlConditionBuilder();
			JObject criteria = (JObject)criteriaValue;
			int i = 0;
			foreach (KeyValuePair<string, JToken> pair in criteria) {
				//TODO restrict to only valid fields

				if (i > 0) {
					query.And();
				}
				query.NewGroup();
				query.Column(pair.Key);
				string value = (string)pair.Value;
				string[] operands = value.Split("OR");
				foreach (string operand in operands) {
					string[] split = operand.Split(" ");
					if (split[0] == "LIKE") {
						query.Like(split[1]);
					} else {
						query.Equals(operand, MySql.Data.MySqlClient.MySqlDbType.String);
					}
					if (operands.Last() != operand) {
						query.Or();
					}
				}
				query.ExitGroup();
				i++;
			}

			//Get products using query and create response object
			List<ProductCategory> selection = wrapper.Select<ProductCategory>(query).ToList();
			JArray foundCategories = new JArray();
			foreach (ProductCategory category in selection) {
				foundCategories.Add(category.Id);
			}

			return new JObject() {
				{"foundProducts", foundCategories}
			};
		}
	}
}