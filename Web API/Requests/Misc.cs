using Logging;
using MySql.Data.MySqlClient;
using MySQLWrapper;
using MySQLWrapper.Data;
using System;
using System.Linq;

namespace API.Requests {
	static partial class RequestMethods {
		public static TechlabMySQL wrapper;
		public static Logger log;
	}
	static partial class Requests {
		public static T getObject<T>(dynamic ID, string column = "ID") where T : SchemaItem, new() {
			MySqlDbType operandtype = MySqlDbType.VarChar;
			if (ID is int) {
				operandtype = MySqlDbType.Int64;
			}

			var selection = RequestMethods.wrapper.Select<T>(new MySqlConditionBuilder()
					.Column(column)
					.Equals((Object)ID, operandtype)
				).ToList();
			if (selection.Count == 0) {
				return null;
			} else {
				return selection[0];
			}
		}
	}
}
