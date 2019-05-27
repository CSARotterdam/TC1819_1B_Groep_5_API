using MySql.Data.MySqlClient;
using System;

namespace MySQLWrapper.Data
{
	/// <summary>
	/// A class representing the data associated with columns of a MySQL database.
	/// </summary>
	class ColumnMetadata
	{
		public string Column { get; }
		public int Length { get; }
		public MySqlDbType Type { get; }

		public ColumnMetadata(string column, int length, MySqlDbType type)
		{
			if (length < 0)
				throw new ArgumentException("Length cannot be lower than 0.", "length");
			Column = column;
			Length = length;
			Type = type;
		}
	}
}