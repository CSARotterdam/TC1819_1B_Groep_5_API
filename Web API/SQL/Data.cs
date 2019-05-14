using MySql.Data.MySqlClient;
using MySQLWrapper.MySQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

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
			Name = name;
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

	abstract class SchemaItem
	{
		/// <summary>
		/// The name of the schema that this item's class is modeled after.
		/// </summary>
		public abstract string Schema { get; }
		/// <summary>
		/// A collection of <see cref="ColumnMetadata"/> objects that represent the columns found in the actual schema.
		/// </summary>
		public abstract ReadOnlyCollection<ColumnMetadata> Metadata { get; }
		/// <summary>
		/// A collection of <see cref="Index"/> objects that represent the indexes found in the actual schema.
		/// </summary>
		public abstract ReadOnlyCollection<Index> Indexes { get; }

		/// <summary>
		/// Gets the array of fields associated with this item.
		/// </summary>
		/// <remarks>
		/// SConsider using the builtin functions for <see cref="Array"/> to alter this array.
		/// </remarks>
		public abstract object[] Fields { get; }

		/// <summary>
		/// Gets the index marked with Auto-Increment, or <c>null</c> if there isn't one.
		/// </summary>
		public Index AutoIncrement
		{
			get
			{
				foreach (var index in Indexes)
					if (index.AutoIncrement) return index;
				return null;
			}
		}

		private object[] fieldTrace = null;

		/// <summary>
		/// Uploads the current object to the database.
		/// </summary>
		/// <param name="connection">An opened <see cref="MySqlConnection"/>.</param>
		/// <returns>The last insert id.</returns>
		public long Upload(MySqlConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				var paramNames = new string[Fields.Length];
				for (int i = 0; i < Fields.Length; i++)
				{
					if (Fields[i] == null)
					{
						paramNames[i] = "NULL";
						continue;
					}
					paramNames[i] = $"@param{i}";
					cmd.Parameters.Add(new MySqlParameter(paramNames[i], Metadata[i].Type) { Value = Fields[i] });
				}

				var columnInsert = string.Join(", ", GetColumns().Select(column => $"`{Schema}`.`{column}`"));
				var valueInsert = string.Join(", ", paramNames);

				cmd.CommandText = SQLConstants.GetInsert(
					Schema,
					columnInsert,
					valueInsert
				);
				cmd.ExecuteNonQuery();
				var scalar = cmd.LastInsertedId;
				if (AutoIncrement != null) Fields[Metadata.IndexOf(AutoIncrement.Columns[0])] = scalar;

				UpdateTrace();
				return scalar;
			}
		}
		/// <summary>
		/// Uploads the current object to the database.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/>.</param>
		/// <returns>The last insert id.</returns>
		public long Upload(TechlabMySQL connection) => connection.Upload(this);

		/// <summary>
		/// Removes the current object from the database.
		/// </summary>
		/// <param name="connection">An opened <see cref="MySqlConnection"/>.</param>
		/// <returns>The number of affected rows.</returns>
		public int Delete(MySqlConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				var condition = new MySqlConditionBuilder(Metadata.ToArray(), Fields);
				cmd.CommandText = SQLConstants.GetDelete(
					Schema,
					condition.ConditionString
				);
				condition.MergeParameters(cmd);

				ClearTrace();
				return cmd.ExecuteNonQuery();
			}
		}
		/// <summary>
		/// Removes the current object from the database.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/>.</param>
		/// <returns>The number of affected rows.</returns>
		public int Delete(TechlabMySQL connection) => connection.Delete(this);

		/// <summary>
		/// Updates the old object in the database with the current object.
		/// </summary>
		/// <param name="connection">An opened <see cref="MySqlConnection"/>.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update(MySqlConnection connection)
		{
			if (fieldTrace == null)
				throw new InvalidOperationException("This object cannot be traced back to the database.");
			using (var cmd = connection.CreateCommand())
			{
				Func<ColumnMetadata, object, string> addValues = (meta, value) =>
				{
					if (value == null) return $"`{meta.Column}` = NULL";
					var paramName = $"@param{cmd.Parameters.Count}";
					cmd.Parameters.Add(new MySqlParameter(paramName, meta.Type) { Value = value });
					return $"`{meta.Column}` = {paramName}";
				};
				var columnValuePairs = string.Join(", ", Metadata.Zip(Fields, (x, y) => addValues(x, y)));
				var condition = new MySqlConditionBuilder(Metadata.ToArray(), fieldTrace);
				cmd.CommandText = SQLConstants.GetUpdate(
					Schema,
					columnValuePairs,
					condition.ConditionString
				);
				condition.MergeParameters(cmd);

				UpdateTrace();
				return cmd.ExecuteNonQuery();
			}
		}
		/// <summary>
		/// Updates the old object in the database with the current object.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/>.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update(TechlabMySQL connection) => connection.Update(this);

		/// <summary>
		/// Selects columns based on the given conditions.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose schema will used in the query.</typeparam>
		/// <param name="connection">An opened <see cref="MySqlConnection"/> object.</param>
		/// <param name="columns">An array specifying which columns to return. Passing <c>null</c> will select all columns.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> filled with the results as object arrays.</returns>
		public static IEnumerable<object[]> Select<T>(MySqlConnection connection, string[] columns, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
				where T : SchemaItem, new()
		{
			using (var cmd = connection.CreateCommand())
			{
				// Because SchemaItem is abstract, I need a reference instance to access certain fields.
				var reference = new T();

				string columnInsert;
				// If no collumns are specified, replace it with a wildcard
				if (columns == null || columns.Length == 0) columnInsert = "*";
				else columnInsert = string.Join(", ", columns.Select(x => x == "*" ? x : $"`{x}`"));

				// Get select query string
				cmd.CommandText = SQLConstants.GetSelect(
					columnInsert,
					$"`{reference.Schema}`",
					condition == null ? "TRUE" : condition.ConditionString
				);
				// Add a limit to the query if specified
				if (range.HasValue) cmd.CommandText += $" LIMIT {range.Value.Start},{range.Value.Amount}";
				// Merge the condition's parameters if it isn't null
				if (condition != null) condition.MergeParameters(cmd);

				foreach (var reader in Core.Read(cmd))
					while (reader.Read())
					{
						// Return all values
						var values = new object[reader.FieldCount];
						reader.GetValues(values);
						yield return values.Select(x => x.GetType() == typeof(DBNull) ? null : x).ToArray();
					}
			}
		}
		/// <summary>
		/// Selects columns based on the given conditions.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose schema will used in the query.</typeparam>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="columns">An array specifying which columns to return. Passing <c>null</c> will select all columns.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> filled with the results as object arrays.</returns>
		public static IEnumerable<object[]> Select<T>(TechlabMySQL connection, string[] columns, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
				where T : SchemaItem, new()
			=> connection.Select<T>(columns, condition, range);

		/// <summary>
		/// Selects all columns of a <see cref="SchemaItem"/> subclass.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose instances will be returned.</typeparam>
		/// <param name="connection">An opened <see cref="MySqlConnection"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <typeparamref name="T"/>.</returns>
		public static IEnumerable<T> Select<T>(MySqlConnection connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null) where T : SchemaItem, new()
		{
			foreach (var result in Select<T>(connection, null, condition, range))
			{
				var obj = new T();
				// Copy the resulting object[] to the new SchemaItem, obj
				result.CopyTo(obj.Fields, 0);
				// Update the 'trace' of the value. The trace links it with its original values.
				obj.UpdateTrace();
				yield return obj;
			}
		}
		/// <summary>
		/// Selects all columns of a <see cref="SchemaItem"/> subclass.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose instances will be returned.</typeparam>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <typeparamref name="T"/>.</returns>
		public static IEnumerable<T> Select<T>(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null) where T : SchemaItem, new()
			=> connection.Select<T>(condition, range);

		/// <summary>
		/// Updates the field trace with the current <see cref="Fields"/>.
		/// <para>This function is called automatically when calling <see cref="Upload(TechlabMySQL)"/> or <see cref="Update(TechlabMySQL)"/>.</para>
		/// </summary>
		/// <remarks>
		/// The field trace is used to update the old instance in the database. Do not call this function if you intend to call <c>Update()</c> later.
		/// </remarks>
		public void UpdateTrace() => fieldTrace = (object[])Fields.Clone();
		/// <summary>
		/// Clears the field trace.
		/// <para>This function is called automatically when calling <see cref="Delete(TechlabMySQL)"/>.</para>
		/// </summary>
		/// <remarks>
		/// The field trace is used to update the old instance in the database. Do not call this function if you intend to call <c>Update()</c> later.
		/// </remarks>
		public void ClearTrace() => fieldTrace = null;

		/// <summary>
		/// Returns a string[] with the column names associated with this SchemaItem.
		/// </summary>
		public string[] GetColumns() => Metadata.Select(x => x.Column).ToArray();
		/// <summary>
		/// Gets an index with the specified name, or null if none were found.
		/// </summary>
		/// <param name="name">The name of the index.</param>
		public Index GetIndex(string name) => Indexes.FirstOrDefault(x => x.Name == name);
		/// <summary>
		/// Gets all indexes of a certain type.
		/// </summary>
		/// <param name="type">The index type to find.</param>
		public Index[] GetIndexesOfType(Index.IndexType type) => Indexes.Where(x => x.Type == type).ToArray();
		
		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		public override string ToString() => GetType().Name + "(" + string.Join(", ", Metadata.Zip(Fields, (x, y) => $"{x.Column}: {y}")) + ")";
	}

	sealed class Item : SchemaItem
	{
		#region Schema Metadata
		private const string _schema = "items";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 11, MySqlDbType.Int32),
			new ColumnMetadata("product", 50, MySqlDbType.VarChar),
			new ColumnMetadata("serial_id", 50, MySqlDbType.VarChar),
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, true, _metadata[0]),
			new Index("product", Index.IndexType.INDEX, _metadata[1]),
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new blank instance of <see cref="Item"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public Item() { }
		/// <summary>
		/// Creates a new instance of <see cref="Item"/>.
		/// </summary>
		/// <param name="id">A unique id to give this item. If null, a unique id will automatically be assigned after uploading.</param>
		/// <param name="product">The id of a <see cref="Product"/></param>
		/// <param name="serial_id">The serial id of the physical item.</param>
		public Item(int? id, string product, string serial_id)
		{
			Id = id;
			ProductId = product;
			SerialId = serial_id;
		}

		#region Properties
		public int? Id
		{
			get { return (int?)Fields[0]; }
			set { _fields[0] = value; }
		}
		public string ProductId
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string SerialId
		{
			get { return (string)Fields[2]; }
			set
			{
				if (value != null && value.Length > Metadata[2].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[2] = value;
			}
		}
		#endregion

		#region SchemaItem Support
		public override string Schema => _schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => _metadata;
		public override ReadOnlyCollection<Index> Indexes => _indexes;
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
			=> Select<Item>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="Item"/>.</returns>
		public static IEnumerable<Item> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Item>(connection, condition, range);
		#endregion

		#region Foreign Key Getters
		/// <summary>
		/// Gets the <see cref="Product"/> with an id equal to <see cref="ProductId"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="Product"/> instance whose Id is equal to <see cref="ProductId"/>, or null if none are found.</returns>
		public Product GetProduct(TechlabMySQL connection)
		{
			var reference = new Product();
			return connection.Select<Product>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { ProductId })).FirstOrDefault();
		}
		#endregion
	}

	sealed class Product : SchemaItem
	{
		#region Schema Metadata
		private const string _schema = "products";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("manufacturer", 80, MySqlDbType.VarChar),
			new ColumnMetadata("category", 50, MySqlDbType.VarChar),
			new ColumnMetadata("name", 50, MySqlDbType.VarChar),
			new ColumnMetadata("image", 50, MySqlDbType.VarChar),
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, true, _metadata[0]),
			new Index("category", Index.IndexType.INDEX, _metadata[1]),
			new Index("name", Index.IndexType.INDEX, _metadata[2]),
			new Index("image", Index.IndexType.INDEX, _metadata[4])
		});
		private readonly object[] _fields = new object[_metadata.Count];
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
		/// <param name="name">The id of a <see cref="LanguageItem"/>.</param>
		/// <param name="image">The id of a <see cref="Data.Image"/>.</param>
		public Product(string id, string manufacturer, string category, string name, string image = "default")
		{
			Id = id;
			Manufacturer = manufacturer;
			Category = category;
			Name = name;
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
		public string Image
		{
			get { return (string)Fields[4]; }
			set
			{
				if (value != null && value.Length > Metadata[4].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[4] = value;
			}
		}
		#endregion

		#region SchemaItem Support
		public override string Schema => _schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => _metadata;
		public override ReadOnlyCollection<Index> Indexes => _indexes;
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

	sealed class ProductCategory : SchemaItem
	{
		#region Schema Metadata
		private const string _schema = "product_categories";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("name", 50, MySqlDbType.VarChar),
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, true, _metadata[0]),
			new Index("name", Index.IndexType.INDEX, _metadata[1])
		});
		private readonly object[] _fields = new object[_metadata.Count];
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
		public override string Schema => _schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => _metadata;
		public override ReadOnlyCollection<Index> Indexes => _indexes;
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

	sealed class LanguageItem : SchemaItem
	{
		#region Schema Metadata
		private const string _schema = "language";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("en", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("nl", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("ar", char.MaxValue, MySqlDbType.Text),
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0])
		});
		private readonly object[] _fields = new object[_metadata.Count];
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
			ISO_en = uniDef;
			for (int i = 2; i < _fields.Length; i++)
				_fields[i] = uniDef;
		}
		/// <summary>
		/// Creates a new instance of <see cref="LanguageItem"/> with translations for some ISO languages.
		/// </summary>
		public LanguageItem(string id, string en, string nl, string ar)
		{
			Id = id;
			ISO_en = en;
			ISO_nl = nl;
			ISO_ar = ar;
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
		public string ISO_en
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string ISO_nl
		{
			get { return (string)Fields[2]; }
			set
			{
				if (value != null && value.Length > Metadata[2].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[2] = value;
			}
		}
		public string ISO_ar
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
		public override string Schema => _schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => _metadata;
		public override ReadOnlyCollection<Index> Indexes => _indexes;
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

	sealed class User : SchemaItem
	{
		public enum UserPermission
		{
			Empty,
			User,
			Collaborator,
			Admin
		}

		#region Schema Metadata
		private const string _schema = "users";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("username", 50, MySqlDbType.VarChar),
			new ColumnMetadata("password", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("permissions", 3, MySqlDbType.Enum),
			new ColumnMetadata("token", 20, MySqlDbType.Int64),
 		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0])
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new instance of <see cref="User"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public User() { }
		/// <summary>
		/// Creates a new instance of <see cref="User"/>.
		/// </summary>
		/// <param name="username">A unique username for this user.</param>
		/// <param name="password">The password to give this user.</param>
		/// <param name="token">The time this user has logged in, represented in seconds since epoch.</param>
		/// <param name="permission">The permissions to give this user.</param>
		public User(string username, string password, long token, UserPermission permission = UserPermission.User)
		{
			Username = username;
			Password = password;
			Permission = permission;
			Token = token;
		}

		#region Properties
		public string Username
		{
			get { return (string)Fields[0]; }
			set
			{
				if (value != null && value.Length > Metadata[0].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[0] = value;
			}
		}
		public string Password
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public UserPermission Permission
		{
			get { return (UserPermission)Enum.Parse(typeof(UserPermission), (string)Fields[2]); }
			set { _fields[2] = value; }
		}
		public long Token
		{
			get { return (long)Fields[3]; }
			set { _fields[3] = value; }
		}
		#endregion

		#region SchemaItem Support
		public override string Schema => _schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => _metadata;
		public override ReadOnlyCollection<Index> Indexes => _indexes;
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
			=> Select<User>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="User"/>.</returns>
		public static IEnumerable<User> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<User>(connection, condition, range);
		#endregion
	}

	class Image : SchemaItem
	{
		#region Schema Metadata
		private const string _schema = "images";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("data", (int)Math.Pow(byte.MaxValue, 3) - 1, MySqlDbType.MediumBlob),
			new ColumnMetadata("extension", 10, MySqlDbType.VarChar),
 		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0])
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Array of image formats supported by android studio.
		/// </summary>
		private static readonly string[] ImageFormats = { ".jpeg", ".jpg", ".gif", ".bmp", ".png", ".webp", ".heif" };

		/// <summary>
		/// Creates a new instance of <see cref="Image"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public Image() { }
		/// <summary>
		/// Creates a new instance of <see cref="Image"/> from an image file on this system.
		/// </summary>
		/// <param name="path">The path to an image file.</param>
		public Image(string path)
			: this(Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path), Path.GetExtension(path).ToLower())
		{ }
		/// <summary>
		/// Creates a new instance of <see cref="Image"/>.
		/// </summary>
		/// <param name="id">The unique identifier to give this image. File extensions are automatically excluded.</param>
		/// <param name="data">The raw byte data of the image file.</param>
		/// <param name="extension">The extension of the image. If null, it will attempt to extract it from <paramref name="id"/>.</param>
		public Image(string id, byte[] data, string extension = null)
		{
			Id = Path.GetFileNameWithoutExtension(id);
			Data = data;
			Extension = extension ?? Path.GetExtension(id);
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
		public byte[] Data
		{
			get { return (byte[])Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string Extension
		{
			get { return (string)Fields[2]; }
			set
			{
				if (value != null && value.Length > Metadata[2].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				if (value != null && !ImageFormats.Contains(value.ToLower()))
					throw new FormatException($"Image format '{value}' is not supported.");
				_fields[2] = value;
			}
		}
		#endregion

		#region SchemaItem Support
		public override string Schema => _schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => _metadata;
		public override ReadOnlyCollection<Index> Indexes => _indexes;
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
			=> Select<Image>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="Image"/>.</returns>
		public static IEnumerable<Image> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Image>(connection, condition, range);
		#endregion
	}
}