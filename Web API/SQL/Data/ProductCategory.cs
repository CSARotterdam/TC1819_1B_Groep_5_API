using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySQLWrapper.Data
{
	sealed class ProductCategory : SchemaItem
	{
		#region Schema Metadata
		public const string schema = "product_categories";
		public static readonly ReadOnlyCollection<ColumnMetadata> metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("name", 50, MySqlDbType.VarChar),
		});
		public static readonly ReadOnlyCollection<Index> indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, metadata[0]),
			new Index("name", Index.IndexType.INDEX, metadata[1])
		});
		private readonly object[] _fields = new object[metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new blank instance of <see cref="ProductCategory"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public ProductCategory() { }
		/// <summary>
		/// Creates a new instance of <see cref="ProductCategory"/>.
		/// </summary>
		/// <param name="id">The id of the category. If null, the id will be assigned upon uploading this item.</param>
		/// <param name="category">A unique name identifier for the category.</param>
		/// <param name="name">The id of a <see cref="LanguageItem"/>.</param>
		public ProductCategory(string id, string name)
		{
			Id = id;
			Name = name;
		}

		#region Properties
		public string Id
		{
			get { return (string)Fields[0]; }
			set { _fields[0] = value; }
		}
		public string Name
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
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
			=> Select<ProductCategory>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="ProductCategory"/>.</returns>
		public static IEnumerable<ProductCategory> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<ProductCategory>(connection, condition, range);
		#endregion

		#region Foreign Key Getters
		/// <summary>
		/// Gets the <see cref="LanguageItem"/> with an id equal to <see cref="Name"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="LanguageItem"/> instance whose Id is equal to <see cref="Name"/>, or null if none are found.</returns>
		public LanguageItem GetName(TechlabMySQL connection)
		{
			var reference = new LanguageItem();
			return connection.Select<LanguageItem>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { Name })).FirstOrDefault();
		}
		#endregion
	}
}