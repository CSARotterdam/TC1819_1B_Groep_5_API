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
        public static JObject updateProduct(JObject request) {
            if (!API.Threads.DatabaseMaintainer.Ping()) {
                return Templates.ServerError("DatabaseConnectionError");
            }

            //TODO Add updateProduct method

            return new JObject() {

            };
        }
    }
}