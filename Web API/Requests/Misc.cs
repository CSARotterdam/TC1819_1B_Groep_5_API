using Logging;
using MySql.Data.MySqlClient;
using MySQLWrapper;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
    static partial class RequestMethods {
        public static TechlabMySQL wrapper;
        public static Logger log;
    }
	static partial class Requests {
		public static T getObject<T>(dynamic ID) where T : SchemaItem, new() {
			MySqlDbType operandtype = MySqlDbType.VarChar;
			if (ID is int) {
				operandtype = MySqlDbType.Int64;
			}

			var selection = RequestMethods.wrapper.Select<T>(new MySqlConditionBuilder()
					.Column("ID")
					.Equals((Object)ID, operandtype)
				).ToList();
			if (selection.Count == 0) {
				return null;
			} else {
				return selection[0];
			}
		}

		/// <summary>
		/// Get a user from the database
		/// </summary>
		/// <param name="username"></param> The username of the user
		/// <returns></returns> The User object of the user. If no user was found, returns null.
		public static User getUser(string username) {
			List<User> selection = RequestMethods.wrapper.Select<User>(new MySqlConditionBuilder()
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
