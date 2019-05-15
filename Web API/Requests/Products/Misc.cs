using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class Requests {
		public static T getObject<T>(string ID) where T: SchemaItem, new() {
			List<T> selection = RequestMethods.wrapper.Select<T>(new MySqlConditionBuilder()
					.Column("ID")
					.Equals()
					.Operand(ID, MySql.Data.MySqlClient.MySqlDbType.VarChar)
			).ToList();
			if (selection.Count == 0) {
				return null;
			} else {
				return selection[0];
			}
		}
	}
}
