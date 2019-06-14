using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		/// <summary>
		/// Handles requests with requestType "GetProductCategories".
		/// </summary>
		/// <remarks>
		/// Optional arguments are:
		///		- Columns > a string[] specifying what fields to return. If omitted or empty, all fields will be returned.
		///		- Criteria > a list of criteria to perform on the specified fields. The keys represent the fields and the values represent the condition.
		///		- Language > a string[] of language codes specifying that translations to return. If empty, all translations will be returned. This replaces the name field.
		///		- Start > specifies the offset of the result set. E.G if set to 2, the first 2 results will be skipped. Default is 0. Value may not be lower than 0.
		///		- Amount > specifies the amount of results to return. Default is ulong.MaxValue. Value may not be lower than 0.
		/// </remarks>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject getProductCategories(JObject request) {
			// Get request arguments
			request.TryGetValue("columns", out JToken requestColumns);
			request.TryGetValue("criteria", out JToken requestCriteria);
			request.TryGetValue("language", out JToken requestLanguages);
			request.TryGetValue("start", out JToken requestRangeStart);
			request.TryGetValue("amount", out JToken requestRangeAmount);

			MySqlConditionBuilder condition = null;

			// Verify the types of the arguments
			List<string> failedVerifications = new List<string>();
			if (requestColumns != null && (requestColumns.Type != JTokenType.Array || requestColumns.Any(x => x.Type != JTokenType.String))) {
				failedVerifications.Add("colums");
			}

			if (requestCriteria != null) {
				try { Misc.CreateCondition((JObject)requestCriteria, condition); } catch (Exception) { failedVerifications.Add("criteria"); }
			}

			if (requestLanguages != null && (requestLanguages.Type != JTokenType.Array || requestLanguages.Any(x => x.Type != JTokenType.String))) {
				failedVerifications.Add("language");
			}

			if (requestRangeStart != null && (requestRangeStart.Type != JTokenType.Integer)) {
				failedVerifications.Add("start");
			}

			if (requestRangeAmount != null && (requestRangeAmount.Type != JTokenType.Integer)) {
				failedVerifications.Add("amount");
			}

			if (failedVerifications.Any()) {
				return Templates.InvalidArguments(failedVerifications.ToArray());
			}

			//Create base response
			var responseData = new JArray();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			// Prepare values for database call
			(ulong, ulong) range = (requestRangeStart?.ToObject<ulong>() ?? 0, requestRangeAmount?.ToObject<ulong>() ?? ulong.MaxValue);
			if (requestColumns == null || requestColumns.Count() == 0) {
				requestColumns = new JArray(ProductCategory.metadata.Select(x => x.Column));
			} else if (requestLanguages != null && !requestColumns.Contains("name")) {
				((JArray)requestColumns).Add("name");
			}

			// Remove unknown language columns
			requestLanguages = new JArray(requestLanguages.Where(x => LanguageItem.metadata.Select(y => y.Column).Contains(x.ToString())));

			// Request category data from database
			List<object[]> categoryData = Connection.Select<ProductCategory>(requestColumns.ToObject<string[]>(), condition, range).ToList();

			// Add all categories as dictionaries to responseData
			foreach (var data in categoryData) {
				var item = new JObject();
				for (int i = 0; i < requestColumns.Count(); i++) {
					item[(string)requestColumns[i]] = new JValue(data[i]);
				}

				responseData.Add(item);
			}

			// Add translations if specified in the arguments
			if (requestLanguages != null) {
				List<string> nameIds = responseData.Select(x => x["name"].ToString()).ToList();

				// Build a condition to get all language items in one query
				bool first = true;
				var nameCondition = new MySqlConditionBuilder();
				foreach (var name in nameIds) {
					if (!first) {
						nameCondition.Or();
					}

					nameCondition.Column("id");
					nameCondition.Equals(name, MySql.Data.MySqlClient.MySqlDbType.String);
					first = false;
				}

				// Get the specified translations
				var languageColumns = requestLanguages.ToObject<List<string>>();
				if (languageColumns.Count == 0) {
					languageColumns.Add("*");
				} else {
					languageColumns.Insert(0, "id");
				}

				List<object[]> names = Connection.Select<LanguageItem>(languageColumns.ToArray(), nameCondition).ToList();
				for (int i = 0; i < responseData.Count; i++) {
					var nameData = names.First(x => x[0].Equals(nameIds[i]));
					var translations = new JObject();
					for (int j = 1; j < languageColumns.Count; j++)
						if (nameData[j] != null)
							translations[languageColumns[j]] = new JValue(nameData[j]);
					responseData[i]["name"] = translations;
				}
			}

			return response;
		}
	}
}