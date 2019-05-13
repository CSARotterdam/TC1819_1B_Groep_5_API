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
        /// <summary>
		/// Handles requests with requestType "getProduct".
		/// </summary>
		/// <param name="request">The request from the client.</param>
		/// <returns>The contents of the requestData field, which is to be returned to the client.</returns>
        
        [verifyPermission(User.UserPermission.User)]
        public static JObject getProduct(JObject request) {
            //Get arguments
            JObject requestData = request["requestData"].ToObject<JObject>();
            requestData.TryGetValue("productID", out JToken idValue);
            requestData.TryGetValue("sendImage", out JToken sendImageValue);
            if (idValue == null || idValue.Type != JTokenType.String) {
                return Templates.MissingArguments;
            }
            if (sendImageValue.Type == JTokenType.Null) {
                sendImageValue = false;
            }

            string ID = idValue.ToString();
            bool sendImage = sendImageValue.ToObject<bool>();

            JObject response = new JObject() {
                {"reason", null },
                {"productData", null}
            };

            //Get product info
            List<Product> products = wrapper.Select<Product>(new MySqlConditionBuilder()
               .Column("id")
               .Equals()
               .Operand(ID, MySql.Data.MySqlClient.MySqlDbType.VarChar)
            ).ToList();
            if (products.Count == 0) {
				response = Templates.NoSuchProduct;
                return response;
            }

            Product product = products[0];

            //Get image, if necessary
            Image image;
            response["productData"] = new JObject() {
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

                response["productData"]["image"] = new JObject() {
                    {"data" , image.Data },
                    {"id", image.Id },
                    {"extension", image.Extension }
                };
            }

            return response;
        }
    }
}