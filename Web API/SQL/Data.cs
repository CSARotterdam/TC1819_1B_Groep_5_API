using System;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MySQLWrapper.Data
{
	enum IndexType
    {
        PRIMARY,
        UNIQUE,
        INDEX
    }

    abstract class SchemaItem
    {
        public abstract string Schema { get; }
        public abstract (string Column, int Length, MySqlDbType Type)[] Metadata { get; }
        public abstract (IndexType IndexType, (string Column, int Length, MySqlDbType Type) Column)[] Indexes { get; }

        public abstract object[] Fields { get; }
        public int Length => Fields.Length;
		public string Primary
		{
			get
			{
				var indexes = GetIndexes(IndexType.PRIMARY);
				if (indexes.Length == 0)
					throw new InvalidOperationException($"Schema `{Schema}` contains no primary key.");
				return indexes[0].Column;
			}
		}

        private object[] fieldTrace = null;

        public void Upload(TechlabMySQL connection)
        {
			
            fieldTrace = Fields;
        }
        public void Delete(TechlabMySQL connection)
        {
            fieldTrace = null;
        }
        public void Update(TechlabMySQL connection)
        {
            fieldTrace = Fields;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="connection">A TechlabMySQL wrapper object.</param>
		/// <param name="columns">An array containing the column names to select.</param>
		/// <param name="conditions">A Dictionary where the key represents a column and the object[] represents conditions to match.
		/// If left null, everything will be selected.</param>
		/// <returns>An IEnumerable containing all rows that matched the conditions.</returns>
		public IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns = null, Dictionary<string, object[]> conditions = null)
		{
			return null;
		}

		public string[] GetColumns()
		{
			var outList = new List<string>();
			foreach (var meta in Metadata)
				outList.Add(meta.Column);
			return outList.ToArray();
		}

		public (string Column, int Length, MySqlDbType Type)[] GetIndexes(IndexType type)
		{
			var outList = new List<(string, int, MySqlDbType)>();
			foreach (var index in Indexes)
				if (index.IndexType == type)
					outList.Add(index.Column);
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

        // TODO: Add static Select method that takes an array of conditions to search for elements in Products
        // Also allows for specification of columns
        // Returns an array filled with lists with dynamic values. (Dynamic because of the potential inconsistency of the column parameters)
        // Sanitation of the columns should be considered.

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
        private static readonly (string, int, MySqlDbType)[] _metadata =
        {
            ("username", 50, MySqlDbType.VarChar),
            ("password", char.MaxValue, MySqlDbType.Text),
            ("permissions", byte.MaxValue, MySqlDbType.Byte),
        };
        private static readonly (IndexType, (string, int, MySqlDbType))[] _indexes = 
        {
            (IndexType.PRIMARY, _metadata[0]),
        };
        private static readonly object[] _fields = new object[_metadata.Length];
        #endregion

        public User(string username, string password, UserPermission permission)
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
            set { _fields[1] = value; }
        }
        #endregion

        #region SchemaItem Support
        public override string Schema => _schema;
        public override (string Column, int Length, MySqlDbType Type)[] Metadata => _metadata;
        public override (IndexType IndexType, (string Column, int Length, MySqlDbType Type) Column)[] Indexes => _indexes;
        public override object[] Fields => _fields;

		public void Select(TechlabMySQL connection, string[] UsernameArgs, string[] PasswordArgs, string[] PermissionArgs)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
