using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests
{
	static partial class RequestMethods
	{
		/// <summary>
		/// Handles requests with requestType "GetProducts".
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
		[verifyPermission(User.UserPermission.User)]
		public static JObject getProducts(JObject request)
		{
			// Get request arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("columns", out JToken requestColumns);
			requestData.TryGetValue("criteria", out JToken requestCriteria);
			requestData.TryGetValue("language", out JToken requestLanguages);
			requestData.TryGetValue("start", out JToken requestRangeStart);
			requestData.TryGetValue("amount", out JToken requestRangeAmount);

			MySqlConditionBuilder condition = new MySqlConditionBuilder();

			// Verify the types of the arguments
			List<string> failedVerifications = new List<string>();
			if (requestColumns != null && (requestColumns.Type != JTokenType.Array || requestColumns.Any(x => x.Type != JTokenType.String)))
				failedVerifications.Add("colums");
			if (requestCriteria != null)
				try
				{ condition = Misc.CreateCondition((JObject)requestCriteria, condition); }
				catch (Exception)
				{ failedVerifications.Add("criteria"); }
			if (requestLanguages != null && (requestLanguages.Type != JTokenType.Array || requestLanguages.Any(x => x.Type != JTokenType.String)))
				failedVerifications.Add("language");
			if (requestRangeStart != null && (requestRangeStart.Type != JTokenType.Integer))
				failedVerifications.Add("start");
			if (requestRangeAmount != null && (requestRangeAmount.Type != JTokenType.Integer))
				failedVerifications.Add("amount");

			if (failedVerifications.Any())
				return Templates.InvalidArguments(failedVerifications.ToArray());

			//Create base response
			var responseData = new JArray();
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			// Prepare values for database call
			if (!condition.IsEmpty()) condition.And();
			condition.Not()
				.Column(Product.indexes.First(x => x.Type == Index.IndexType.PRIMARY).Columns[0].Column)
				.Equals(0, MySqlDbType.Int32);
			(ulong, ulong) range = (requestRangeStart?.ToObject<ulong>() ?? 0, requestRangeAmount?.ToObject<ulong>() ?? ulong.MaxValue);
			if (requestColumns == null || requestColumns.Count() == 0) requestColumns = new JArray(Product.metadata.Select(x => x.Column));
			else if (requestLanguages != null && !requestColumns.Contains("name")) ((JArray)requestColumns).Add("name");

			// Request category data from database
			List<object[]> categoryData = wrapper.Select<Product>(requestColumns.ToObject<string[]>(), condition, range).ToList();

			// Add all categories as dictionaries to responseData
			foreach (var data in categoryData)
			{
				var item = new JObject();
				for (int i = 0; i < requestColumns.Count(); i++)
					item[(string)requestColumns[i]] = new JValue(data[i]);
				responseData.Add(item);
			}

			// Add translations if specified in the arguments
			if (requestLanguages != null)
			{
				List<string> nameIds = responseData.Select(x => x["name"].ToString()).ToList();

				// Build a condition to get all language items in one query
				bool first = true;
				var nameCondition = new MySqlConditionBuilder();
				foreach (var name in nameIds)
				{
					if (!first) nameCondition.Or();
					nameCondition.Column("id");
					nameCondition.Equals(name, MySqlDbType.String);
					first = false;
				}
				// If the condition is empty, insert a condition that is false
				//if (first) nameCondition.Not().Null().Is().Null();

				// Get the specified translations
				var languageColumns = requestLanguages.ToObject<List<string>>();
				if (languageColumns.Count == 0) languageColumns.AddRange(LanguageItem.metadata.Select(x => x.Column));
				else languageColumns.Insert(0, "id");
				List<object[]> names = wrapper.Select<LanguageItem>(languageColumns.ToArray(), nameCondition).ToList();
				for (int i = 0; i < responseData.Count; i++)
				{
					var nameData = names.First(x => x[0].Equals(nameIds[i]));
					var translations = new JObject();
					for (int j = 1; j < languageColumns.Count; j++)
						translations[languageColumns[j]] = new JValue(nameData[j]);
					responseData[i]["name"] = translations;
				}
			}

			return response;
		}
	}
}