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
        public static JObject addProduct(JObject request) {
            //Get arguments
            JObject requestData = request["requestData"].ToObject<JObject>();
            if (
                !requestData.TryGetValue("productInfo", out JToken infoValue)
            ) {
                return Templates.MissingArguments;
            }
            if (infoValue.Type == JTokenType.Null) {
                return Templates.MissingArguments;
            }

            //Create base response
            JObject response = new JObject() {
                {"reason", null },
                {"success", false}
            };

            //Check if product already exists
            List<Product> products = wrapper.Select<Product>(new MySqlConditionBuilder()
               .Column("id")
               .Equals()
               .Operand(requestData["productInfo"]["id"], MySql.Data.MySqlClient.MySqlDbType.VarChar)
            ).ToList();
            if (products.Count != 0) {
				response = Templates.NoSuchProduct;
                return response;
            }

            //Create product object
            JObject productInfo = (JObject)requestData["productInfo"];
            Product product = new Product();


            return new JObject() {
                {"requestData", new JObject(){
                }}
            };
        }
    }
}