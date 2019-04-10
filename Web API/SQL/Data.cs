using System;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using MySQLWrapper.MySQL;

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
			if (autoIncrement && NumberTypes.Contains(columns[0].Type))
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
        public abstract ColumnMetadata[] Metadata { get; }
        public abstract Index[] Indexes { get; }
        public abstract object[] Fields { get; }

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

		public ulong Upload(MySqlConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				var paramNames = new string[Fields.Length];
				for (int i = 0; i < Fields.Length; i++)
				{
					paramNames[i] = $"@param{i}";
					cmd.Parameters.Add(new MySqlParameter(paramNames[i], Metadata[i].Type) { Value = Fields[i] });
				}

				var columnInsert = string.Join(", ", GetColumns().Select(column => $"`{Schema}`.`{column}`"));
				var valueInsert = string.Join(", ", paramNames);

				cmd.CommandText = SQLConstants.Insert
					.Replace("<schema>", Schema)
					.Replace("<columns>", columnInsert)
					.Replace("<values>", valueInsert);
				cmd.CommandText += "; " + SQLConstants.SelectLastIndex;

				var scalar = (ulong)cmd.ExecuteScalar();
				if (AutoIncrement != null) Fields[Array.IndexOf(Metadata, AutoIncrement.Columns[0])] = scalar;

				UpdateTrace();
				return scalar;
			}
		}
		public ulong Upload(TechlabMySQL connection) => connection.Upload(this);

		public int Delete(MySqlConnection connection)
		{
			throw new NotImplementedException(); // TODO: Implement Delete method.
		}
		public int Delete(TechlabMySQL connection) => connection.Delete(this);

		public int Update(MySqlConnection connection)
		{
			throw new NotImplementedException(); // TODO: Implement Update method.
		}
		public int Update(TechlabMySQL connection) => connection.Update(this);

		/// <summary>
		/// Updates the field trace with the current <see cref="Fields"/>.
		/// <para>This function is called automatically when calling <see cref="Upload(TechlabMySQL)"/> or <see cref="Update(TechlabMySQL)"/>.</para>
		/// <para>The field trace is used to update the old instance in the database. Do not call this function if you intend to call <c>Update()</c> later.</para>
		/// </summary>
		public void UpdateTrace() => fieldTrace = (object[])Fields.Clone();
		/// <summary>
		/// Clears the field trace.
		/// <para>This function is called automatically when calling <see cref="Delete(TechlabMySQL)"/>.</para>
		/// <para>The field trace is used to update the old instance in the database. Do not call this function if you intend to call <c>Update()</c> later.</para>
		/// </summary>
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
	}

    sealed class Item
	{
		public const string Schema = "`items`";
		public const string Primary = IdName;

		#region Field Names
		public const string IdName = "`id`";
		public const string ProductName = "`product`";
		public const string SerialIdName = "`serial_id`";
		#endregion
		#region Lengths
		public const int IdLength = 11;
		public const int ProductLength = 50;
		public const int SerialIdLength = 30;
		#endregion
		#region Types
		public const MySqlDbType IdType = MySqlDbType.Int32;
		public const MySqlDbType ProductType = Product.IdType;
		public const MySqlDbType SerialIdType = MySqlDbType.VarChar;
		#endregion

		private int _id = -1;

		public int Id {
			get
			{
				if (!HasPrimary()) throw new InvalidOperationException("This object has no id.");
				return _id;
			}
			set
			{
				if (value < 0) throw new ArgumentException("Value cannot be smaller than 0.", "value");
				_id = value;
			}
		}
		public char[] ProductId { get; set; }
		public char[] SerialId { get; set; }

		public Item(IEnumerable<char> product, IEnumerable<char> serialId)
		{
			if (product.Count() > ProductLength) throw new ArgumentException("Length cannot be larger than " + ProductLength, "product");
			if (serialId.Count() > SerialIdLength) throw new ArgumentException("Length cannot be larger than " + SerialIdLength, "serialId");
			ProductId = product.ToArray();
			SerialId = serialId.ToArray();
		}
		public Item(Product product, IEnumerable<char> serialId) : this(product.Id, serialId)
		{
			if (product == null) throw new NullReferenceException("Product cannot be null.");
		}
		public Item(int id, IEnumerable<char> product, IEnumerable<char> serialId) : this(product, serialId)
		{
			if (id < 0) throw new ArgumentException("Value cannot be smaller than 0.", "id");
			Id = id;
		}
		public Item(int id, Product product, IEnumerable<char> serialId) : this(id, product.Id, serialId)
		{
			if (product == null) throw new NullReferenceException("Product cannot be null.");
		}

		public Product GetProduct(TechlabMySQL connection) => connection.GetProduct(ProductId);

		public void ClearId() => _id = -1;
		public bool HasPrimary() => _id != -1;
	}

    sealed class Product
	{
		public const string Schema = "`products`";
		public const string Primary = IdName;

		#region Field Names
		public const string IdName = "`id`";
		public const string ManufacturerName = "`manufacturer`";
		public const string CategoryName = "`category`";
		public const string NameName = "`name`"; // lol
		#endregion
		#region Lengths
		public const int IdLength = 50;
		public const int ManufacturerLength = char.MaxValue;
		public const int CategoryLength = ProductCategory.IdLength;
		public const int NameLength = LanguageItem.IdLength; 
		#endregion
		#region Types
		public const MySqlDbType IdType = MySqlDbType.VarChar;
		public const MySqlDbType ManufacturerType = MySqlDbType.Text;
		public const MySqlDbType CategoryType = ProductCategory.IdType;
		public const MySqlDbType NameType = LanguageItem.IdType; 
		#endregion

		public char[] Id { get; set; }
		public string Manufacturer { get; set; }
		public int Category { get; set; }
		public char[] Name { get; set; }

		public Product(IEnumerable<char> id, string manufacturer, int category, IEnumerable<char> name)
		{
			if (id.Count() > IdLength) throw new ArgumentException("Length cannot be larger than " + IdLength, "id");
			if (manufacturer.Length > ManufacturerLength) throw new ArgumentException("Length cannot be larger than " + ManufacturerLength, "manufacturer");
			if (name.Count() > NameLength) throw new ArgumentException("Length cannot be larger than " + NameLength, "name");
			Id = id.ToArray();
			Manufacturer = manufacturer;
			Category = category;
			Name = name.ToArray();
		}
		public Product(IEnumerable<char> id, string manufacturer, ProductCategory category, LanguageItem name) : this(id, manufacturer, category.Id, name.Id) { }

		public ProductCategory GetCategory(TechlabMySQL connection) => connection.GetCategory(Category);
		public LanguageItem GetName(TechlabMySQL connection) => connection.GetLanguageItem(Name);
	}

    sealed class ProductCategory
	{
		public const string Schema = "`product_categories`";
		public const string Primary = IdName;

		#region Field Names
		public const string IdName = "`id`";
		public const string CategoryName = "`category`";
		public const string NameName = "`name`"; // lol, again
		#endregion
		#region Lengths
		public const int IdLength = 11;
		public const int CategoryLength = 50;
		public const int NameLength = LanguageItem.IdLength;
		#endregion
		#region Types
		public const MySqlDbType IdType = MySqlDbType.Int32;
		public const MySqlDbType CategoryType = MySqlDbType.VarChar;
		public const MySqlDbType NameType = LanguageItem.IdType; 
		#endregion

		private int _id = -1;

		public int Id
		{
			get
			{
				if (!HasPrimary()) throw new InvalidOperationException("This object has no id.");
				return _id;
			}
			set
			{
				if (value < 0) throw new ArgumentException("Value cannot be smaller than 0.", "value");
				_id = value;
			}
		}
		public char[] Category { get; set; }
		public char[] Name { get; set; }

		public ProductCategory(IEnumerable<char> category, IEnumerable<char> name)
		{
			if (category.Count() > CategoryLength) throw new ArgumentException("Length cannot be larger than " + CategoryLength, "category");
			if (name.Count() > NameLength) throw new ArgumentException("Length cannot be larger than " + NameLength, "name");
			Category = category.ToArray();
			Name = name.ToArray();
		}
		public ProductCategory(IEnumerable<char> category, LanguageItem name) : this(category, name.Id) { }
		public ProductCategory(int id, IEnumerable<char> category, IEnumerable<char> name) : this(category, name)
		{
			if (id < 0) throw new ArgumentException("Value cannot be smaller than 0.", "id");
			Id = id;
		}
		public ProductCategory(int id, IEnumerable<char> category, LanguageItem name) : this(id, category, name.Id) { }

		public LanguageItem GetName(TechlabMySQL connection) => connection.GetLanguageItem(Name);

		public void ClearId() => _id = -1;
		public bool HasPrimary() => _id != -1;
	}

	sealed class LanguageItem
	{
		public const string Schema = "`language`";
		public const string Primary = IdName;

		#region Field Names
		public const string IdName = "`id`";
		public const string ISO_enName = "`en`";
		public const string ISO_nlName = "`nl`";
		#endregion
		#region Lengths
		public const int IdLength = 50;
		public const int ISO_enLength = char.MaxValue;
		public const int ISO_nlLength = char.MaxValue;
		#endregion
		#region Types
		public const MySqlDbType IdType = MySqlDbType.VarChar;
		public const MySqlDbType ISO_enType = MySqlDbType.Text;
		public const MySqlDbType ISO_nlType = MySqlDbType.Text; 
		#endregion

		public char[] Id { get; set; }
		public string ISO_en { get; set; }
		public string ISO_nl { get; set; }

		public LanguageItem(IEnumerable<char> id, string iso_en, string iso_nl)
		{
			if (id.Count() > IdLength) throw new ArgumentException("Length cannot be larger than " + IdLength, "id");
			if (iso_en.Length > ISO_enLength) throw new ArgumentException("Length cannot be larger than " + ISO_enLength, "iso_en");
			if (iso_nl.Length > ISO_nlLength) throw new ArgumentException("Length cannot be larger than " + ISO_nlLength, "iso_nl");
			Id = id.ToArray();
			ISO_en = iso_en;
			ISO_nl = iso_nl;
		}
		public LanguageItem(IEnumerable<char> id, string definition) : this(id, definition, definition) { }
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
        private static readonly ColumnMetadata[] _metadata =
        {
            new ColumnMetadata("username", 50, MySqlDbType.VarChar),
			new ColumnMetadata("password", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("permissions", byte.MaxValue, MySqlDbType.Byte),
        };
        private static readonly Index[] _indexes = 
        {
            new Index("PRIMARY", Index.IndexType.PRIMARY, _metadata[0])
        };
        private static readonly object[] _fields = new object[_metadata.Length];
        #endregion

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
        public override ColumnMetadata[] Metadata => _metadata;
        public override Index[] Indexes => _indexes;
        public override object[] Fields => _fields;
		#endregion

		#region Methods
		public void Select(TechlabMySQL connection, MySqlConditionBuilder condition)
		{
			throw new NotImplementedException(); // TODO: Implement Select with a MySqlConditionBuilder.
			// Calls the Verify function of the ConditionBuilder with _metadata.
		}
		public void Select(TechlabMySQL connection, string[] usernameArgs, string[] passwordArgs, string[] permissionArgs)
		{
			throw new NotImplementedException(); // TODO: Implement Select with a set of arrays as conditions.
			// Creates a MySqlConditionBuilder and calls the other select.
		}
		#endregion
	}
}
