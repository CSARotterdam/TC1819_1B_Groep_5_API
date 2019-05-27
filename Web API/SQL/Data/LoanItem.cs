using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySQLWrapper.Data
{
	class LoanItem : SchemaItem
	{
		#region Schema Metadata
		public const string schema = "loans";
		public static readonly ReadOnlyCollection<ColumnMetadata> metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 11, MySqlDbType.Int32),
			new ColumnMetadata("user", 50, MySqlDbType.VarChar),
			new ColumnMetadata("product_item", 10, MySqlDbType.VarChar),
			new ColumnMetadata("start", 0, MySqlDbType.DateTime),
			new ColumnMetadata("end", 0, MySqlDbType.DateTime),
			new ColumnMetadata("is_item_acquired", 1, MySqlDbType.Int16)
 		});
		public static readonly ReadOnlyCollection<Index> indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, true, metadata[0]),
			new Index("user", Index.IndexType.INDEX, metadata[1]),
			new Index("product", Index.IndexType.INDEX, metadata[2])
		});
		private readonly object[] _fields = new object[metadata.Count];
		#endregion

		/// <summary>
		/// Creates a new instance of <see cref="LoanItem"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public LoanItem() { }
		/// <summary>
		/// Creates a new instance of <see cref="LoanItem"/>.
		/// </summary>
		/// <param name="id">The unique id to give to this loaditem. If null, a unique id will be chosen after uploading.</param>
		/// <param name="user">The id of the user whom this loan belongs to.</param>
		/// <param name="productItem">The id of the item that is being loaned.</param>
		/// <param name="start">The date when this loan starts. This must be less than <paramref name="end"/>.</param>
		/// <param name="end">The date when this loan ends.</param>
		/// <param name="isAcquired">Set whether or not the item associated with this loan has been aquired by the user.</param>
		public LoanItem(int? id, string user, int productItem, DateTime start, DateTime end, bool isAcquired = false)
		{
			if (DateTime.Compare(start, end) > 0)
				throw new ArgumentException("The start date must be less than the end date.");
			Id = id;
			User = user;
			ProductItem = productItem;
			Start = start;
			End = end;
			IsAcquired = isAcquired;
		}

		#region Properties
		public int? Id
		{
			get { return (int)Fields[0]; }
			set { _fields[0] = value; }
		}
		public string User
		{
			get { return (string)Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public int ProductItem
		{
			get { return (int)Fields[2]; }
			set { _fields[2] = value; }
		}
		public DateTime Start
		{
			get { return (DateTime)Fields[3]; }
			set { _fields[3] = value; }
		}
		public DateTime End
		{
			get { return (DateTime)Fields[4]; }
			set { _fields[4] = value; }
		}
		public bool IsAcquired
		{
			get { return (bool)Fields[5]; }
			set { _fields[5] = value; }
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

		#region Foreign Key Getters
		/// <summary>
		/// Gets the <see cref="Data.User"/> object associated with this <see cref="LoanItem"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="Data.User"/> instance whose id is equal to <see cref="User"/>, or null if none are found.</returns>
		public User GetUser(TechlabMySQL connection)
		{
			var reference = new User();
			return connection.Select<User>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { User })).FirstOrDefault();
		}

		/// <summary>
		/// Gets the <see cref="Data.ProductItem"/> object associated with this <see cref="LoanItem"/>.
		/// </summary>
		/// <param name="connection">The connection to perform the query on.</param>
		/// <returns>A <see cref="Data.ProductItem"/> instance whose id is equal to <see cref="ProductItem"/>, or null if none are found.</returns>
		public ProductItem GetProductItem(TechlabMySQL connection)
		{
			var reference = new ProductItem();
			return connection.Select<ProductItem>(new MySqlConditionBuilder(reference.GetIndexesOfType(Index.IndexType.PRIMARY).First().Columns, new object[] { ProductItem })).FirstOrDefault();
		}
		#endregion
	}
}