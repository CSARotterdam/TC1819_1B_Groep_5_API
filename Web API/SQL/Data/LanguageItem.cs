using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MySQLWrapper.Data
{
	sealed class LanguageItem : SchemaItem
	{
		#region Schema Metadata
		public const string schema = "language";
		public static readonly ReadOnlyCollection<ColumnMetadata> metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 80, MySqlDbType.VarChar),
			new ColumnMetadata("en", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("nl", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("ar", char.MaxValue, MySqlDbType.Text),
		});
		public static readonly ReadOnlyCollection<Index> indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, metadata[0])
		});
		private readonly object[] _fields = new object[metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new blank instance of <see cref="LanguageItem"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public LanguageItem() { }
		/// <summary>
		/// Creates a new instance <see cref="LanguageItem"/> with a single universal definition.
		/// </summary>
		/// <param name="id">A unique identifier for this object.</param>
		/// <param name="uniDef">A universal definition to assign to this languageitem.</param>
		public LanguageItem(string id, string uniDef)
		{
			Id = id;
			en = uniDef;
			for (int i = 2; i < _fields.Length; i++)
				_fields[i] = uniDef;
		}
		/// <summary>
		/// Creates a new instance of <see cref="LanguageItem"/> with translations for some ISO languages.
		/// </summary>
		public LanguageItem(string id, string en, string nl, string ar)
		{
			Id = id;
			this.en = en;
			this.nl = nl;
			this.ar = ar;
		}

		#region Properties
		public string Id
		{
			get { return (string)Fields[0]; }
			set
			{
				if (value != null && value.Length > Metadata[0].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[0] = value;
			}
		}
		public string en
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string nl
		{
			get { return (string)Fields[2]; }
			set
			{
				if (value != null && value.Length > Metadata[2].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[2] = value;
			}
		}
		public string ar
		{
			get { return (string)Fields[3]; }
			set
			{
				if (value != null && value.Length > Metadata[3].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[3] = value;
			}
		}
		#endregion

		#region SchemaItem Support
		public override string Schema => schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => metadata;
		public override ReadOnlyCollection<Index> Indexes => indexes;
		public override object[] Fields => _fields;
		#endregion

		#region Methods
		/// <summary>
		/// Selects columns based on the given conditions.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="columns">An array specifying which columns to return. Passing <c>null</c> will select all columns.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> filled with the results as object arrays.</returns>
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<LanguageItem>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="LanguageItem"/>.</returns>
		public static IEnumerable<LanguageItem> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<LanguageItem>(connection, condition, range);
		#endregion
	}
}