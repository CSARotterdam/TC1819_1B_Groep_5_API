using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MySQLWrapper.MySQL
{
	static class Core
	{
		public static IEnumerable<IDataReader> Read(MySqlCommand command)
		{
			using (var reader = command.ExecuteReader())
			{
				do yield return reader;
				while (reader.NextResult());
			}
		}
		public static IEnumerable<IDataReader> Read(MySqlConnection connection, string sql)
		{
			using (var command = new MySqlCommand(sql, connection))
			{
				return Read(command);
			}
		}
		
		public static IEnumerable<IDictionary<string, string>> GetForeignKeys(MySqlConnection connection, string table)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = SQLConstants.SelectForeignKeys;
				command.Parameters.Add("@table", MySqlDbType.String).Value = table;

				var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var dict = new Dictionary<string, string>();
					for (int i = 0; i < reader.FieldCount; i++)
						dict.Add(reader.GetName(i), reader.GetString(i));
					yield return dict;
				}
			}
		}
	}
}