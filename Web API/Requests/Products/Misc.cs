using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class Requests {
		public static T getObject<T>(dynamic ID) where T : SchemaItem, new() {
			MySqlDbType operandtype = MySqlDbType.VarChar;
			if(ID is int){
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
	}
}
