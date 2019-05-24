using MySql.Data.MySqlClient;
using System;
using System.Linq;

namespace MySQLWrapper.Data
{
	/// <summary>
	/// A class representing MySQL indexes.
	/// </summary>
	class Index
	{
		/// <summary>
		/// An collection of types that support auto increment.
		/// </summary>
		private static readonly MySqlDbType[] NumberTypes =
		{
			MySqlDbType.Byte,
			MySqlDbType.Int16,
			MySqlDbType.Int24,
			MySqlDbType.Int32,
			MySqlDbType.Int64,
			MySqlDbType.UByte,
			MySqlDbType.UInt16,
			MySqlDbType.UInt24,
			MySqlDbType.UInt32,
			MySqlDbType.UInt64,
			MySqlDbType.Double,
			MySqlDbType.Float,
			MySqlDbType.Decimal,
			MySqlDbType.NewDecimal
		};

		/// <summary>
		/// An enumeration of available index types.
		/// </summary>
		public enum IndexType
		{
			PRIMARY,
			UNIQUE,
			INDEX
		}

		public string Name { get; }
		public IndexType Type { get; }
		public ColumnMetadata[] Columns { get; }
		public bool AutoIncrement { get; }

		/// <summary>
		/// Creates a new instance of <see cref="Index"/>.
		/// </summary>
		/// <param name="name">The name of the index.</param>
		/// <param name="type">The type of the index.</param>
		/// <param name="columns">A collection of <see cref="ColumnMetadata"/> instances.</param>
		public Index(string name, IndexType type, params ColumnMetadata[] columns)
		{
			if (columns.Length == 0)
				throw new ArgumentException("Cannot create index with no columns.", "columns");
			Type = type;
			Columns = columns;
			Name = Type == IndexType.PRIMARY ? "PRIMARY" : name;
			AutoIncrement = false;
		}
		/// <summary>
		/// Creates a new instance of <see cref="Index"/>.
		/// </summary>
		/// <param name="name">The name of the index.</param>
		/// <param name="type">The type of the index.</param>
		/// <param name="autoIncrement">Whether or not the index is marked as auto increment.</param>
		/// <param name="columns">A collection of <see cref="ColumnMetadata"/> instances. If auto increment is true, this may only include one column.</param>
		public Index(string name, IndexType type, bool autoIncrement, params ColumnMetadata[] columns)
		{
			if (columns.Length == 0)
				throw new ArgumentException("Cannot create index with no columns.", "columns");
			if (autoIncrement && columns.Length > 1)
				throw new ArgumentException("Cannot assign auto increment to index with multiple columns.");
			if (autoIncrement && !NumberTypes.Contains(columns[0].Type))
				throw new ArgumentException("Cannot assign auto increment to a column with the type " + columns[0].Type);
			Type = type;
			Columns = columns;
			Name = name;
			AutoIncrement = autoIncrement;
		}
	}
}