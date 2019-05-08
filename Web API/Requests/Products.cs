using MySQLWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class RequestMethods {
		public static JObject getProduct(JObject request) {
            //If the requestType isn't "getProduct", throw an exception.
            if (request["requestType"].ToString() != "getProduct") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

            //Get arguments
            String ID;
            String username;
            long token;
            try {
                ID = request["requestData"]["productID"].ToString();
                username = request["requestData"]["username"].ToString();
                token = (long)request["requestData"]["token"]; 
            } catch (ArgumentException) {
                return Templates.MissingArguments;
            }

            //Check token
            if (!checkToken(getUser(username), token)) {
                return Templates.ExpiredToken;
            }

            List<Product> selection = wrapper.Select<Product>(new MySqlConditionBuilder()
               .Column("id")
               .Equals()
               .Operand(ID, MySql.Data.MySqlClient.MySqlDbType.VarChar)
           ).ToList();

            JObject response = new JObject() {
				{"requestData", new JObject(){
                    {"reason", null },
                    {"productData", null}
				}}
			};

            if(selection.Count == 0) {
                response["requestData"]["reason"] = "Product not found.";
            } else {
                response["requestData"]["productData"] = JsonConvert.SerializeObject(selection[0].Fields);  
            }
            return response;
        }

        public static JObject getProductList(JObject request) {
            //If the requestType isn't "getProductList", throw an exception.
            if (request["requestType"].ToString() != "getProductList") {
                throw new InvalidRequestTypeException(request["requestType"].ToString());
            }

            //TODO Add getProduct method

            return new JObject() {
                {"requestID", request["requestID"].ToString()},
                {"requestData", new JObject(){
                }}
            };
        }

        public static JObject updateProduct(JObject request) {
            //If the requestType isn't "updateProduct", throw an exception.
            if (request["requestType"].ToString() != "updateProduct") {
				throw new InvalidRequestTypeException(request["requestType"].ToString());
			}

			//TODO Add updateProduct method

			return new JObject() {
				{"requestID", request["requestID"].ToString()},
				{"requestData", new JObject(){
				}}
			};
		}

        public static JObject addProduct(JObject request) {
            //If the requestType isn't "addProduct", throw an exception.
            if (request["requestType"].ToString() != "addProduct") {
                throw new InvalidRequestTypeException(request["requestType"].ToString());
            }

            //TODO Add updateProduct method

            return new JObject() {
                {"requestID", request["requestID"].ToString()},
                {"requestData", new JObject(){
                }}
            };
        }

        public static JObject deleteObject(JObject request) {
            //If the requestType isn't "deleteObject", throw an exception.
            if (request["requestType"].ToString() != "deleteObject") {
                throw new InvalidRequestTypeException(request["requestType"].ToString());
            }

            //TODO Add updateProduct method


             return new JObject() {
                {"requestID", request["requestID"].ToString()},
                {"requestData", new JObject(){
                }}
            };
        }
    }
}