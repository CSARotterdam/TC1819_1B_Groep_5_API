using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySQLWrapper.Data
{
	sealed class Product : SchemaItem
	{
		#region Schema Metadata
		public const string schema = "products";
		public static readonly ReadOnlyCollection<ColumnMetadata> metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("manufacturer", 80, MySqlDbType.VarChar),
			new ColumnMetadata("category", 50, MySqlDbType.VarChar),
			new ColumnMetadata("name", 50, MySqlDbType.VarChar),
			new ColumnMetadata("description", 50, MySqlDbType.VarChar),
			new ColumnMetadata("image", 50, MySqlDbType.VarChar),
		});
		public static readonly ReadOnlyCollection<Index> indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, metadata[0]),
			new Index("category", Index.IndexType.INDEX, metadata[2]),
			new Index("name", Index.IndexType.INDEX, metadata[3]),
			new Index("description", Index.IndexType.INDEX, metadata[4]),
			new Index("image", Index.IndexType.INDEX, metadata[5])
		});
		private readonly object[] _fields = new object[metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new blank instance of <see cref="Product"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public Product() { }
		/// <summary>
		/// Creates a new instance of <see cref="Product"/>.
		/// </summary>
		/// <param name="id">A unique id to give this product.</param>
		/// <param name="manufacturer">The name of the manufacturer.</param>
		/// <param name="category">The id of a <see cref="ProductCategory"/>.</param>
		/// <param name="name">The id of a <see cref="LanguageItem"/> representing the name of this product.</param>
		/// <param name="name">The id of a <see cref="LanguageItem"/> representing the description of this product.</param>
		/// <param name="image">The id of a <see cref="Data.Image"/>.</param>
		public Product(string id, string manufacturer, string category, string name, string description, string image = "default")
		{
			Id = id;
			Manufacturer = manufacturer;
			Category = category;
			Name = name;
			Description = description;
			Image = image;
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
		public string Manufacturer
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string Category
		{
			get { return (string)Fields[2]; }
			set
			{
				if (value != null && value.Length > Metadata[2].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[2] = value;
			}
		}
		public string Name
		{
			get { return (string)Fields[3]; }
			set
			{
				if (value != null && value.Length > Metadata[3].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[3] = value;
			}
		}
		public string Description
		{
			get { return (string)Fields[4]; }
			set
			{
				if (value != null && value.Length > Metadata[4].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[4] = value;
			}
		}
		public string Image
		{
			get { return (string)Fields[5]; }
			set
			{
				if (value != null && value.Length > Metadata[5].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[5] = value;
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
			=> Select<Product>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="Product"/>.</returns>
		public static IEnumerable<Product> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Product>(connection, condition, range);
		#endregion

		#region Foreign Key Getters
		/// <summary>
		/// Gets the <see cref="ProductCategory"/> with an id equal to <see cref="Category"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="ProductCategory"/> instance whose Id is equal to <see cref="Category"/>, or null if none are found.</returns>
		public ProductCategory GetCategory(TechlabMySQL connection)
		{
			var reference = new ProductCategory();
			return connection.Select<ProductCategory>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { Category })).FirstOrDefault();
		}
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
		/// <summary>
		/// Gets the <see cref="LanguageItem"/> with an id equal to <see cref="Description"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="LanguageItem"/> instance whose Id is equal to <see cref="Description"/>, or null if none are found.</returns>
		public LanguageItem GetDescription(TechlabMySQL connection)
		{
			var reference = new LanguageItem();
			return connection.Select<LanguageItem>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { Description })).FirstOrDefault();
		}
		/// <summary>
		/// Gets the <see cref="Data.Image"/> with an id equal to <see cref="Image"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="Data.Image"/> instance whose Id is equal to <see cref="Image"/>, or null if none are found.</returns>
		public Image GetImage(TechlabMySQL connection)
		{
			var reference = new Image();
			return connection.Select<Image>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { Image })).FirstOrDefault();
		}
		#endregion
	}
}