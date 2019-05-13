using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class RequestMethods {
       
        /// <summary>
        /// Given a user and token, checks if the token is valid.
        /// </summary>
        /// <param name="user">A user object</param>
        /// <param name="tokenRaw">A long containing the raw token</param>
        /// <returns>Bool, which is true if the token is valid.</returns>
        private static bool checkToken(User user, long tokenRaw) {
            if (user.Token != tokenRaw) {
                return false;
            }

            System.DateTime token = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            token = token.AddSeconds(tokenRaw).ToLocalTime();
            return !((DateTime.Today - token).TotalSeconds > (double)Program.Settings["authenticationSettings"]["expiration"]);
        }

        /// <summary>
        /// Checks if a user can use requests with the specified permission level right now.
        /// </summary>
        /// <param name="user"></param>The user object, as stored in the database.
        /// <param name="">permission</param>The lowest User.UserPermission enum a user should have.
        /// <param name="token"></param>The user token that was received from the client.
        /// <returns></returns>
        private static bool checkPermission(User user, User.UserPermission permission) {
            return user.Permission >= permission;
        }

        /// <summary>
        /// Get a user from the database
        /// </summary>
        /// <param name="username"></param> The username of the user
        /// <returns></returns> The User object of the user. If no user was found, returns null.
        public static User getUser(string username) {
            List<User> selection = wrapper.Select<User>(new MySqlConditionBuilder()
                   .Column("Username")
                   .Equals()
                   .Operand(username, MySql.Data.MySqlClient.MySqlDbType.VarChar)
            ).ToList();

            if (selection.Count == 0) {
                return null;
            } else {
                return selection[0];
            }
        }
	}
}
