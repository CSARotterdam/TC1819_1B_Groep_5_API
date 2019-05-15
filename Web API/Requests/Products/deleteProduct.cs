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

        [verifyPermission(User.UserPermission.Collaborator)]
        public static JObject deleteProduct(JObject request) {
            //Get arguments
            JObject requestData = request["requestData"].ToObject<JObject>();
            requestData.TryGetValue("productID", out JToken idValue);
            if (idValue == null || idValue.Type != JTokenType.String) {
				return Templates.MissingArguments("productID");
            }

            string productID = idValue.ToString();

			//Check if product exists
			Product product = Requests.getObject<Product>(productID);
			if (product == null) {
				return Templates.NoSuchProduct;
			}

			product.Delete(wrapper);
			product.GetImage(wrapper).Delete(wrapper);
			product.GetName(wrapper).Delete(wrapper);

			//Create base response
			return new JObject() {
				{"reason", null },
				{"success", true}
			};

		}
	}
}