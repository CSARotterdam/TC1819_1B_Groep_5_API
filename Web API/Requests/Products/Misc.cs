using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class Requests {
		/// <summary>
		/// Get a user from the database
		/// </summary>
		/// <param name="username"></param> The username of the user
		/// <returns></returns> The User object of the user. If no user was found, returns null.
		public static Product getProduct(string ID) {
			List<Product> selection = RequestMethods.wrapper.Select<Product>(new MySqlConditionBuilder()
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

		public static ProductCategory getProductCategory(string ID) {
			List<ProductCategory> selection = RequestMethods.wrapper.Select<ProductCategory>(new MySqlConditionBuilder()
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
