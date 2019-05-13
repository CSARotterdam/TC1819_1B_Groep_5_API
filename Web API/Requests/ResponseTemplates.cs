using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
    public static class Templates {
        public static JObject MissingArguments = new JObject() {
            {"reason", "MissingArguments" }
        };
        public static JObject ExpiredToken = new JObject() {
            {"reason", "ExpiredToken"}
        };
        public static JObject InvalidRequestType = new JObject() {
            {"reason", "InvalidRequestType"}
        };
        public static JObject AccessDenied = new JObject() {
            {"reason", "AccessDenied"}
        };
        public static JObject InvalidLogin = new JObject() {
            {"reason" , "InvalidLogin"}
        };
		public static JObject NoSuchProduct = new JObject() {
			{"reason", "NoSuchProduct" }
		};

        public static JObject ServerError(string message) {
            return new JObject() {
                {"reason", "ServerError"},
                {"message", message}
            };
        }
    }
}
