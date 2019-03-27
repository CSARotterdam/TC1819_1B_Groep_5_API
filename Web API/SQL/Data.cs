using System;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MySQLWrapper.Data
{
	class Item
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

	class Product
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

	class ProductCategory
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

	class LanguageItem
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
}
