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
			requestData.TryGetValue("language", out JToken languageValue);
            if (idValue == null || idValue.Type != JTokenType.String || languageValue == null || languageValue.Type != JTokenType.Array) {
				return Templates.MissingArguments("productID, language");
            }
            if (sendImageValue == null || sendImageValue.Type == JTokenType.Null) {
                sendImageValue = false;
            }

            string ID = idValue.ToString();
            bool sendImage = sendImageValue.ToObject<bool>();

			//Create base response
            JObject response = new JObject() {
                {"reason", null },
                {"productData", null}
            };

			//Get product info
			Product product = Requests.getProduct(ID);
            if (product == null) {
				response = Templates.NoSuchProduct;
                return response;
            }

            //Add product data to response
            response["productData"] = new JObject() {
				{"id",  product.Id},
				{"manufacturer", product.Manufacturer},
				{"category", product.Category},
				{"name", new JObject(){
					{ "en", null },
					{ "nl", null },
					{ "ar", null },
				}},
				{"image", null }
			};

			//Add product names, if any.
			LanguageItem item = product.GetName(wrapper);
			List<string> language = requestData["language"].ToObject<List<string>>();
			if (language.Contains("en")) {
				response["productData"]["name"]["en"] = item.en;
			}
			if (language.Contains("nl")) {
				response["productData"]["name"]["nl"] = item.nl;
			}
			if (language.Contains("ar")) {
				response["productData"]["name"]["ar"] = item.ar;
			}

			//Get image, if necessary
			Image image;
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