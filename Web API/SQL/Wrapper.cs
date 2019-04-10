using System;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using MySQLWrapper.MySQL;

namespace MySQLWrapper
{
	class TechlabMySQL : IDisposable
	{
		private readonly MySqlConnection connection;
		
		/// <summary>
		/// Creates a new instance of TechlabMySQL.
		/// </summary>
		/// <param name="server">The server's url.</param>
		/// <param name="port">The server's port.</param>
		/// <param name="username">Username for logging in to the server.</param>
		/// <param name="password">The password for logging in to the server.</param>
		/// <param name="database">(Optional) The desired database. Can be changed with <seealso cref="ChangeDatabase(string)"/></param>
		/// <param name="timeout">(Optional) The connection timeout in seconds. -1 by default.</param>
		/// <param name="persistLogin">(Optional) Set whether to stay logged in for an extended amount of time. Off by default.</param>
		/// <param name="logging">(Optional) Set logging on or off. Off by default.</param>
		public TechlabMySQL(string server, string port, string username = null, string password = null, string database = null, int timeout = -1, bool persistLogin = false, bool logging = false)
		{
			if (server == null) throw new ArgumentNullException("server");
			if (port == null) throw new ArgumentNullException("port");

			var builder = new MySqlConnectionStringBuilder();
			builder.Add("server", server);
			builder.Add("port", port);
			if (username != null) builder.Add("username", username);
			if (password != null) builder.Add("password", password);
			if (database != null) builder.Add("database", database);
			if (timeout > 0) builder.Add("connect timeout", timeout);
			builder.Add("persist security info", persistLogin);
			builder.Add("logging", logging);

			connection = new MySqlConnection(builder.GetConnectionString(true));
		}

		public ulong Upload(SchemaItem item) => item.Upload(connection);

		public int Update(SchemaItem item) => item.Update(connection);

		public int Delete(SchemaItem item) => item.Delete(connection);

		#region Database getters
		/// <summary>
		/// Constructs an Item object from data in the database.
		/// </summary>
		/// <param name="primaryCondition">The string regex to match the primary key to.</param>
		/// <returns>A Item object with an id matching "primaryCondition", or null if no match was found.</returns>
		public Item GetItem(int primaryCondition)
		{
			var reader = GenericGetter(Item.Schema, Item.Primary, new Tuple<MySqlDbType, object>(Item.IdType, primaryCondition));
			if (reader == null) return null;
			return new Item(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
		}

		/// <summary>
		/// Constructs a Product object from data in the database.
		/// </summary>
		/// <param name="primaryCondition">The string regex to match the primary key to.</param>
		/// <returns>A Product object with an id matching "primaryCondition", or null if no match was found.</returns>
		public Product GetProduct(IEnumerable<char> primaryCondition)
		{
			var reader = GenericGetter(Product.Schema, Product.Primary, new Tuple<MySqlDbType, object>(Product.IdType, new string(primaryCondition.ToArray())));
			if (reader == null) return null;
			return new Product(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3));
		}

		/// <summary>
		/// Constructs a ProductCategory from data within the database.
		/// </summary>
		/// <param name="primaryCondition">The string regex to match the primary key to.</param>
		/// <returns>A ProductCategory object with an id matching "primaryCondition".</returns>
		public ProductCategory GetCategory(int primaryCondition)
		{
			var reader = GenericGetter(ProductCategory.Schema, ProductCategory.Primary, new Tuple<MySqlDbType, object>(ProductCategory.IdType, primaryCondition));
			if (reader == null) return null;
			return new ProductCategory(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
		}

		/// <summary>
		/// Constructs a LanguageItem object from data in the database.
		/// </summary>
		/// <param name="primaryCondition">The string regex to match the primary key to.</param>
		/// <returns>A LanguageItem object with an id matching "primaryCondition", or null if no match was found.</returns>
		public LanguageItem GetLanguageItem(IEnumerable<char> primaryCondition)
		{
			var reader = GenericGetter(LanguageItem.Schema, LanguageItem.Primary, new Tuple<MySqlDbType, object>(LanguageItem.IdType, new string(primaryCondition.ToArray())));
			if (reader == null) return null;
			return new LanguageItem(reader.GetString(0), reader.GetString(1), reader.GetString(2));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="schema">The string of the target schema to search.</param>
		/// <param name="primary">The name of the primary key.</param>
		/// <param name="primaryCondition">A type and value pair to match the primary key to.</param>
		/// <returns>A MySqlDataReader object positioned on the row of matched data, or null of no match was found.</returns>
		private MySqlDataReader GenericGetter(string schema, string primary, Tuple<MySqlDbType, object> primaryCondition)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = SQLConstants.ConditionalSelect1
					.Replace("<table>", schema)
					.Replace("<primary>", primary);
				command.Parameters.Add(new MySqlParameter("@condition", primaryCondition.Item1)).Value = primaryCondition.Item2;

				var readerEnum = Core.Read(command).GetEnumerator();
				if (!readerEnum.MoveNext()) return null;

				var reader = (MySqlDataReader)readerEnum.Current;
				if (!reader.Read()) return null;

				return reader;
			}
		}
		#endregion
		#region Database inserters

		/// <summary>
		/// Inserts an Item object into the database.
		/// 
		/// If the primary key (id) is left unspecified, it will be updated to it's new id after it's insertion.
		/// </summary>
		/// <param name="item">The new Item object to insert into the database.</param>
		public void InsertItem(Item item)
		{
			var fields = new string[] { Item.IdName, Item.ProductName, Item.SerialIdName };
			var values = new Tuple<MySqlDbType, object>[]
			{
				new Tuple<MySqlDbType, object>(Item.IdType, (item.HasPrimary() ? (object)item.Id : null)),
				new Tuple<MySqlDbType, object>(Item.ProductType, new string(item.ProductId)),
				new Tuple<MySqlDbType, object>(Item.SerialIdType, new string(item.SerialId))
			};
			item.Id = GenericInsert(Item.Schema, fields, values);
		}

		/// <summary>
		/// Inserts a Product object into the database.
		/// </summary>
		/// <param name="product">The new Product object to insert into the database.</param>
		public void InsertProduct(Product product)
		{
			var fields = new string[] { Product.IdName, Product.ManufacturerName, Product.CategoryName, Product.NameName };
			var values = new Tuple<MySqlDbType, object>[]
			{
				new Tuple<MySqlDbType, object>(Product.IdType, product.Id),
				new Tuple<MySqlDbType, object>(Product.ManufacturerType, product.Manufacturer),
				new Tuple<MySqlDbType, object>(Product.CategoryType, product.Category),
				new Tuple<MySqlDbType, object>(Product.NameType, product.Name)
			};
			GenericInsert(Product.Schema, fields, values);
		}

		/// <summary>
		/// Inserts a ProductCategory object into the database.
		/// 
		/// If the primary key (id) is left unspecified, it will be updated to it's new id after it's insertion.
		/// </summary>
		/// <param name="category">The new ProductCategory object to insert into the database.</param>
		public void InsertCategory(ProductCategory category)
		{
			var fields = new string[] { ProductCategory.IdName, ProductCategory.CategoryName, ProductCategory.NameName };
			var values = new Tuple<MySqlDbType, object>[]
			{
				new Tuple<MySqlDbType, object>(ProductCategory.IdType, (category.HasPrimary() ? (object)category.Id : null)),
				new Tuple<MySqlDbType, object>(ProductCategory.CategoryType, category.Category),
				new Tuple<MySqlDbType, object>(ProductCategory.NameType, new string(category.Name))
			};
			category.Id = GenericInsert(ProductCategory.Schema, fields, values);
		}

		/// <summary>
		/// Inserts a LanguageItem object into the database.
		/// </summary>
		/// <param name="item">The new LanguageItem object to insert into the database.</param>
		public void InsertLanguageItem(LanguageItem item)
		{
			var fields = new string[] { LanguageItem.IdName, LanguageItem.ISO_enName, LanguageItem.ISO_nlName };
			var values = new Tuple<MySqlDbType, object>[]
			{
				new Tuple<MySqlDbType, object>(LanguageItem.IdType, item.Id),
				new Tuple<MySqlDbType, object>(LanguageItem.ISO_enType, item.ISO_en),
				new Tuple<MySqlDbType, object>(LanguageItem.ISO_nlType, item.ISO_nl)
			};
			GenericInsert(LanguageItem.Schema, fields, values);
		}

		/// <summary>
		/// Takes a schema, set of fields and list of type and value pairs to execute a generic SQL insert command.
		/// </summary>
		/// <param name="schema">The name of the schema to insert into.</param>
		/// <param name="fields">An array containing the names of the fields to set. Must be equal in length of "values".</param>
		/// <param name="values">An array containing pairs of data types and values. Must be equal in length of "fields".</param>
		/// <returns>The last insert id. Useful for primary keys with auto-increment. Returns 0 in all other cases.</returns>
		private int GenericInsert(string schema, string[] fields, Tuple<MySqlDbType, object>[] values)
		{
			if (fields.Length != values.Length) throw new ArgumentException("Fields and values must be equal in length.");

			using (var command = connection.CreateCommand())
			{
				var fieldsRepl = "";
				var valuesRepl = "";
				for (int i = 0; i < fields.Length; i++)
				{
					fieldsRepl += (i == 0 ? "" : ", ") + fields[i];
					valuesRepl += (i == 0 ? "" : ", ") + "@param" + i;
				}

				command.CommandText = SQLConstants.Insert
					.Replace("<table>", schema)
					.Replace("<fields>", fieldsRepl)
					.Replace("<values>", valuesRepl);

				for (int i = 0; i < values.Length; i++)
					command.Parameters.Add(new MySqlParameter("@param" + i, values[i].Item1)).Value = values[i].Item2;
				
				command.CommandText += ";" + SQLConstants.SelectLastIndex;
				return (int)(long)(ulong)command.ExecuteScalar();
			}
		}

		#endregion

		#region Exposed connection properties

		public bool IsPasswordExpired => connection.IsPasswordExpired;

		#endregion
		#region Exposed connection methods

		public void Open() => connection.Open();
		public async void OpenAsync() => await connection.OpenAsync();

		public void Close() => connection.Close();
		public async void CloseAsync() => await connection.CloseAsync();

		public void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);

		public bool Ping() => connection.Ping();
		
		#endregion

		#region IDisposable Support

		private bool disposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					connection.Dispose();
				}
				disposed = true;
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}
