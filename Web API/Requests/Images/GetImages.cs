using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		/// <summary>
		/// Gets and returns a list of images from the database.
		/// </summary>
		/// <remarks>
		/// This function accepts two arguments: ([] means optional)
		///		[columns] > a list of field names to return.
		///		images > A list of image ids whose associated image to return.
		/// </remarks>
		/// <param name="request"></param>
		/// <returns></returns>
		[RequiresPermissionLevel(UserPermission.User)]
		public JObject getImages(JObject request) {
			//Get arguments
			request.TryGetValue("columns", out JToken requestColumns);
			request.TryGetValue("images", out JToken requestImageIds);

			if (requestImageIds == null) {
				return Templates.MissingArguments("imageIds");
			}

			// Verify arguments
			List<string> failedVerifications = new List<string>();
			if (requestColumns != null && (requestColumns.Type != JTokenType.Array || ((JArray)requestColumns).Count == 0)) {
				failedVerifications.Add("columns");
			}

			if (requestImageIds.Type != JTokenType.Array) {
				failedVerifications.Add("images");
			}

			if (failedVerifications.Any()) {
				return Templates.InvalidArguments(failedVerifications.ToArray());
			}

			// Build condition
			var condition = new MySqlConditionBuilder();
			bool first = true;
			foreach (string id in requestImageIds) {
				if (!first) {
					condition.Or();
				}

				condition.Column(Image.indexes.First(x => x.Type == Index.IndexType.PRIMARY).Columns[0].Column);
				condition.Equals(id, MySqlDbType.String);
				first = false;
			}
			// If condition is blank, add a condition that is false
			if (first) {
				condition.Not().Null().Is().Null();
			}

			// Prepare query values
			if (requestColumns == null || !requestColumns.Any()) {
				requestColumns = new JArray(Image.metadata.Select(x => x.Column));
			}
			// Add primary key column name
			((JArray)requestColumns).Insert(0, Image.indexes.First(x => x.Type == Index.IndexType.PRIMARY).Columns[0].Column);

			// Get images
			List<object[]> imageData = Connection.Select<Image>(requestColumns.ToObject<string[]>(), condition).ToList();

			//Create base response
			var responseData = new JObject();
			var response = new JObject() {
				{"reason", null },
				{"responseData", responseData }
			};

			foreach (var data in imageData) {
				var item = new JObject();
				for (int i = 1; i < requestColumns.Count(); i++) {
					item.Add((string)requestColumns[i], new JValue(data[i]));
				}

				responseData.Add((string)data[0], item);
			}

			return response;
		}
	}
}