﻿using MySQLWrapper.Data;
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

		[verifyPermission(User.UserPermission.Collaborator)]
		public static JObject deleteProductCategory(JObject request) {
			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			requestData.TryGetValue("categoryID", out JToken idValue);
			if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
			}

			string categoryID = idValue.ToString();

			//Check if product exists
			ProductCategory category = Requests.getObject<ProductCategory>(categoryID);
			if (category == null) {
				return Templates.NoSuchProductCategory;
			}

			category.Delete(wrapper);
			category.GetName(wrapper).Delete(wrapper);

			//Create base response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};

		}
	}
}