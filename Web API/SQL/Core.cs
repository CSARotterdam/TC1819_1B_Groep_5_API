using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;

namespace MySQLWrapper.MySQL
{
	/// <summary>
	/// Handles basic SQL communication
	/// </summary>
	static class Core
	{
		/// <summary>
		/// Reads the output of the command. Yields one IDataReader per resultset.
		/// </summary>
		/// <param name="command">The <see cref="MySqlCommand"/> to excecute.</param>
		/// <returns>An IEnumerable containing IDataReaders.</returns>
		public static IEnumerable<IDataReader> Read(MySqlCommand command)
		{
			using (var reader = command.ExecuteReader())
			{
				do yield return reader;
				while (reader.NextResult());
			}
		}
		/// <summary>
		/// Reads the output of the query. Yields one IDataReader per resultset.
		/// </summary>
		/// <param name="connection">The MySql connection to send the query to.</param>
		/// <param name="sql">The MySql query.</param>
		/// <returns>An IEnumerable containing IDataReaders.</returns>
		public static IEnumerable<IDataReader> Read(MySqlConnection connection, string sql)
		{
			using (var command = new MySqlCommand(sql, connection))
			{
				return Read(command);
			}
		}
	}
}