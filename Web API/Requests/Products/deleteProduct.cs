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
                return Templates.MissingArguments;
            }

            string ID = idValue.ToString();

            //Create base response
            JObject response = new JObject() {
                {"reason", null },
                {"success", false}
            };

            //Get product info
            List<Product> products = wrapper.Select<Product>(new MySqlConditionBuilder()
               .Column("id")
               .Equals()
               .Operand(ID, MySql.Data.MySqlClient.MySqlDbType.VarChar)
            ).ToList();
            if (products.Count == 0) {
                response["requestData"]["reason"] = "NoSuchProduct.";
                return response;
            }

            Product product = products[0];
            product.Delete(wrapper);
            response["requestData"]["success"] = true;

            return response;
        }
    }
}