using MySql.Data.MySqlClient;
using MySQLWrapper.MySQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySQLWrapper.Data
{
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
	class Index
	{
		/// <summary>
		/// An array of types that support auto increment.
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

		public Index(string name, IndexType type, params ColumnMetadata[] columns)
		{
			if (columns.Length == 0)
				throw new ArgumentException("Cannot create index with no columns.", "columns");
			Type = type;
			Columns = columns;
			Name = name;
			AutoIncrement = false;
		}
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
		public abstract string Schema { get; }
		public abstract ReadOnlyCollection<ColumnMetadata> Metadata { get; }
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
		public static IEnumerable<object[]> Select<T>(MySqlConnection connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
				where T : SchemaItem, new()
		{
			using (var cmd = connection.CreateCommand())
			{
				var reference = new T();

				string columnInsert;
				if (columns == null || columns.Length == 0) columnInsert = "*";
				else columnInsert = string.Join(", ", columns.Select(x => x == "*" ? x : $"`{x}`"));

				cmd.CommandText = SQLConstants.GetSelect(
					columnInsert,
					$"`{reference.Schema}`",
					condition == null ? "TRUE" : condition.ConditionString
				);
				if (range.HasValue) cmd.CommandText += $" LIMIT {range.Value.Start},{range.Value.Amount}";
				if (condition != null) condition.MergeParameters(cmd);

				foreach (var reader in Core.Read(cmd))
					while (reader.Read())
					{
						var values = new object[reader.FieldCount];
						reader.GetValues(values);
						yield return values;
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
		public static IEnumerable<object[]> Select<T>(TechlabMySQL connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
				where T : SchemaItem, new()
		{
			return connection.Select<T>(columns, condition, range);
		}

		/// <summary>
		/// Selects all columns of a <see cref="SchemaItem"/> subclass.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose instances will be returned.</typeparam>
		/// <param name="connection">An opened <see cref="MySqlConnection"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <typeparamref name="T"/>.</returns>
		public static IEnumerable<T> SelectAll<T>(MySqlConnection connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null) where T : SchemaItem, new()
		{
			foreach (var result in Select<T>(connection, null, condition, range))
			{
				var obj = new T();
				result.CopyTo(obj.Fields, 0);
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
		public static IEnumerable<T> SelectAll<T>(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null) where T : SchemaItem, new()
			=> connection.SelectAll<T>(condition, range);

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

		public string[] GetColumns()
		{
			var outList = new List<string>();
			foreach (var meta in Metadata)
				outList.Add(meta.Column);
			return outList.ToArray();
		}
		public Index GetIndex(string name)
		{
			foreach (var index in Indexes)
				if (index.Name == name)
					return index;
			return null;
		}
		public Index[] GetIndexesOfType(Index.IndexType type)
		{
			var outList = new List<Index>();
			foreach (var index in Indexes)
				if (index.Type == type)
					outList.Add(index);
			return outList.ToArray();
		}
		
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
		/// Creates a new <see cref="Item"/> instance.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public Item() { }
		public Item(int? id, string product, string serial_id)
		{
			Id = id;
			ProductId = product;
			SerialId = serial_id;
		}

		#region Properties
		public int? Id
		{
			get { return (int)Fields[0]; }
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
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Item>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="Item"/>.</returns>
		public static IEnumerable<Item> SelectAll(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> SelectAll<Item>(connection, condition, range);
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
			new ColumnMetadata("category", 11, MySqlDbType.Int32),
			new ColumnMetadata("name", 50, MySqlDbType.VarChar),
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0]),
			new Index("category", Index.IndexType.INDEX, _metadata[1]),
			new Index("name", Index.IndexType.INDEX, _metadata[2])
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new <see cref="Product"/> instance.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public Product() { }
		public Product(string id, string manufacturer, int category, string name)
		{
			Id = id;
			Manufacturer = manufacturer;
			Category = category;
			Name = name;
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
		public int Category
		{
			get { return (int)Fields[2]; }
			set { _fields[2] = value; }
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
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Product>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="Product"/>.</returns>
		public static IEnumerable<Product> SelectAll(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> SelectAll<Product>(connection, condition, range);
		#endregion
	}

	sealed class ProductCategory : SchemaItem
	{
		#region Schema Metadata
		private const string _schema = "product_categories";
		private static readonly ReadOnlyCollection<ColumnMetadata> _metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 11, MySqlDbType.Int32),
			new ColumnMetadata("category", 50, MySqlDbType.VarChar),
			new ColumnMetadata("name", 50, MySqlDbType.VarChar),
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, true, _metadata[0]),
			new Index("category", Index.IndexType.UNIQUE, _metadata[1]),
			new Index("name", Index.IndexType.INDEX, _metadata[2])
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new <see cref="ProductCategory"/> instance.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public ProductCategory() { }
		public ProductCategory(int? id, string category, string name)
		{
			Id = id;
			Category = category;
			Name = name;
		}

		#region Properties
		public int? Id
		{
			get { return (int)Fields[0]; }
			set { _fields[0] = value; }
		}
		public string Category
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string Name
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
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<ProductCategory>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="ProductCategory"/>.</returns>
		public static IEnumerable<ProductCategory> SelectAll(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> SelectAll<ProductCategory>(connection, condition, range);
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
			new ColumnMetadata("nl", char.MaxValue, MySqlDbType.Text)
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0])
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new <see cref="LanguageItem"/> instance.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public LanguageItem() { }
		public LanguageItem(string id, string en, string nl)
		{
			Id = id;
			ISO_en = en;
			ISO_nl = nl;
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
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<LanguageItem>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="LanguageItem"/>.</returns>
		public static IEnumerable<LanguageItem> SelectAll(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> SelectAll<LanguageItem>(connection, condition, range);
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
		});
		private static readonly ReadOnlyCollection<Index> _indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0])
		});
		private readonly object[] _fields = new object[_metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new <see cref="User"/> instance.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public User() { }
		public User(string username, string password, UserPermission permission = UserPermission.User)
		{
			Username = username;
			Password = password;
			Permission = permission;
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
			get { return (UserPermission)Fields[2]; }
			set { _fields[2] = value; }
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
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns = null, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<User>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="User"/>.</returns>
		public static IEnumerable<User> SelectAll(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> SelectAll<User>(connection, condition, range);
		#endregion
	}
}