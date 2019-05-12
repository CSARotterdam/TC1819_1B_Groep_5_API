using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace API.Requests {
    static partial class RequestMethods {
        public static JObject getProduct(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
            //If the requestType isn't "getProduct", throw an exception.
            if (request["requestType"].ToString() != "getProduct") {
                return Templates.InvalidRequestType;
            }

            //Get arguments
            JObject requestData = request["requestData"].ToObject<JObject>();
            if (
                !requestData.TryGetValue("username", out JToken usernameValue) ||
                !requestData.TryGetValue("token", out JToken tokenValue) ||
                !requestData.TryGetValue("productID", out JToken idValue) ||
                !requestData.TryGetValue("sendImage", out JToken sendImageValue)
            ){
                return Templates.MissingArguments;
            }
            if (usernameValue.Type == JTokenType.Null || tokenValue.Type == JTokenType.Null || idValue.Type == JTokenType.Null) {
                return Templates.MissingArguments;
            }
            if (sendImageValue.Type == JTokenType.Null) {
                sendImageValue = false;
            }

            string ID = idValue.ToString();
            bool sendImage = sendImageValue.ToObject<bool>();

            //Check token
            try {
                string username = usernameValue.ToString();
                long token = tokenValue.ToObject<long>();
                if (!checkToken(getUser(username), token)) {
                    return Templates.ExpiredToken;
                }
            } catch (FormatException) {
                return Templates.ExpiredToken;
            }

            JObject response = new JObject() {
                {"requestData", new JObject(){
                    {"reason", null },
                    {"productData", null}
                }}
            };

            //Get product info
            List<Product> products = wrapper.Select<Product>(new MySqlConditionBuilder()
               .Column("id")
               .Equals()
               .Operand(ID, MySql.Data.MySqlClient.MySqlDbType.VarChar)
            ).ToList();
            if(products.Count == 0) {
                response["requestData"]["reason"] = "NoSuchProduct.";
                return response;
            }

            Product product = products[0];

            //Get image, if necessary
            Image image;
            response["requestData"]["productData"] = new JObject() {
                {"id",  product.Id},
                {"manufacturer", product.Manufacturer},
                {"category", product.Category},
                {"name", product.Name},
                {"image", null }
            };

            if (sendImage) {
                List<Image> images = wrapper.Select<Image>(new MySqlConditionBuilder()
                   .Column("id")
                   .Equals()
                   .Operand(product.Image, MySql.Data.MySqlClient.MySqlDbType.VarChar)
                ).ToList();
                image = images[0];

                response["requestData"]["productData"]["image"] = new JObject() {
                    {"data" , image.Data },
                    {"id", image.Id },
                    {"extension", image.Extension }
                };
            }

            return response;
        }

        public static JObject getProductList(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
            //If the requestType isn't "getProductList", throw an exception.
            if (request["requestType"].ToString() != "getProductList") {
                return Templates.InvalidRequestType;
            }

            //Get arguments
            JObject requestData = request["requestData"].ToObject<JObject>();
            if (
                !requestData.TryGetValue("username", out JToken usernameValue) ||
                !requestData.TryGetValue("token", out JToken tokenValue) ||
                !requestData.TryGetValue("criteria", out JToken criteriaValue)
            ){
                return Templates.MissingArguments;
            }
            if (usernameValue.Type == JTokenType.Null || tokenValue.Type == JTokenType.Null || criteriaValue.Type == JTokenType.Null) {
                return Templates.MissingArguments;
            }



            //Check token
            try {
                string username = usernameValue.ToString();
                long token = tokenValue.ToObject<long>();
                if (!checkToken(getUser(username), token)) {
                    return Templates.ExpiredToken;
                }
            } catch(FormatException) {
                return Templates.ExpiredToken;
            }

            //Parse criteria and use them to build a query;
            MySqlConditionBuilder query = new MySqlConditionBuilder();
            JObject criteria = (JObject)criteriaValue;
            int i = 0;
            foreach(KeyValuePair<string, JToken> pair in criteria) {
                if(i > 0) {
                    query.And();
                }
                query.NewGroup();
                query.Column(pair.Key);
                string value = (string)pair.Value;
                string[] operands = value.Split("OR");
                foreach(string operand in operands) {
                    string[] split = operand.Split(" ");
                    if(split[0] == "LIKE") {
                        query.Like(split[1]);
                    } else {
                        query.Equals(operand, MySql.Data.MySqlClient.MySqlDbType.String);
                    }
                    if(operands.Last() != operand) {
                        query.Or();
                    }
                }
                query.ExitGroup();
                i++;
            }

            //Get products using query and create response object
            List<Product> selection = wrapper.Select<Product>(query).ToList();
            JArray foundProducts = new JArray();
            foreach(Product product in selection) {
                foundProducts.Add(product.Id);
            }
            
            return new JObject() {
                {"requestData", new JObject(){
                    {"foundProducts", foundProducts}
                }}
            };
        }

        public static JObject updateProduct(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
            //If the requestType isn't "updateProduct", throw an exception.
            if (request["requestType"].ToString() != "updateProduct") {
                return Templates.InvalidRequestType;
            }


            //TODO Add updateProduct method

            return new JObject() {
				{"requestData", new JObject(){
				}}
			};
		}

        public static JObject addProduct(JObject request) {
            //If the requestType isn't "addProduct", throw an exception.
            if (request["requestType"].ToString() != "addProduct") {
                return Templates.InvalidRequestType;
            }


            //TODO Add updateProduct method

            return new JObject() {
                {"requestData", new JObject(){
                }}
            };
        }

        public static JObject deleteProduct(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }
            //If the requestType isn't "deleteObject", throw an exception.
            if (request["requestType"].ToString() != "deleteProduct") {
                return Templates.InvalidRequestType;
            }

            //Get arguments
            JObject requestData = request["requestData"].ToObject<JObject>();
            if (
                !requestData.TryGetValue("username", out JToken usernameValue) ||
                !requestData.TryGetValue("token", out JToken tokenValue) ||
                !requestData.TryGetValue("productID", out JToken idValue)
            ) {
                return Templates.MissingArguments;
            }
            if (usernameValue.Type == JTokenType.Null || tokenValue.Type == JTokenType.Null || idValue.Type == JTokenType.Null) {
                return Templates.MissingArguments;
            }

            string ID = idValue.ToString();

            //Check user
            try {
                string username = usernameValue.ToString();
                long token = tokenValue.ToObject<long>();
                User user = getUser(username);
                if (!checkToken(user, token)) {
                    return Templates.ExpiredToken;
                }
                if (!(user.Permission >= User.UserPermission.Collaborator)) {
                    return Templates.AccessDenied;
                }
            } catch (FormatException) {
                return Templates.ExpiredToken;
            }

            //Create base response
            JObject response = new JObject() {
                {"requestData", new JObject(){
                    {"reason", null },
                    {"success", false}
                }}
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