using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Requests {
    public static class Templates {
        public static JObject MissingArguments = new JObject() {
            {"requestData", new JObject(){
                {"reason", "MissingArguments" }
            }}
        };
        public static JObject ExpiredToken = new JObject() {
            {"requestData", new JObject(){
                {"reason", "InvalidToken"}
            }}
        };
        public static JObject InvalidRequestType = new JObject() {
            {"requestData", new JObject(){
                {"reason", "InvalidRequestType"}
            }}
        };
        public static JObject AccessDenied = new JObject() {
            {"requestData", new JObject(){
                {"reason", "AccessDenied"}
            }}
        };

        public static JObject ServerError(string message) {
            return new JObject() {
                {"requestData", new JObject(){
                    {"reason", "ServerError"},
                    {"message", message}
                }}
            };
        }
    }
}
