using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using MySQLWrapper.MySQL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySQLWrapper
{
	class TechlabMySQL : IDisposable
	{
		private MySqlConnection Connection => RaiseIfDisposed()._connection;
		private readonly MySqlConnection _connection;
		
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

			var builder = new MySqlConnectionStringBuilder {
				{ "server", server },
				{ "port", port }
			};
			if (username != null) builder.Add("username", username);
			if (password != null) builder.Add("password", password);
			if (database != null) builder.Add("database", database);
			if (timeout > 0) builder.Add("connect timeout", timeout);
			builder.Add("persist security info", persistLogin);
			builder.Add("logging", logging);

			_connection = new MySqlConnection(builder.GetConnectionString(true));
		}

		public long Upload(SchemaItem item) => item.Upload(Connection);
		public int Update(SchemaItem item) => item.Update(Connection);
		public int Delete(SchemaItem item) => item.Delete(Connection);

		/// <summary>
		/// Selects columns based on the given conditions.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose schema will used in the query.</typeparam>
		/// <param name="columns">An array specifying which columns to return. Passing <c>null</c> will select all columns.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> filled with the results as object arrays.</returns>
		public IEnumerable<object[]> Select<T>(string[] columns, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null) where T : SchemaItem, new()
			=> SchemaItem.Select<T>(Connection, columns, condition, range);
		/// <summary>
		/// Selects all columns of a <see cref="SchemaItem"/> subclass.
		/// </summary>
		/// <typeparam name="T">The <see cref="SchemaItem"/> subclass whose instances will be returned.</typeparam>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <typeparamref name="T"/>.</returns>
		public IEnumerable<T> Select<T>(MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null) where T : SchemaItem, new()
			=> SchemaItem.SelectAll<T>(Connection, condition, range);

		#region Exposed connection properties

		public bool IsPasswordExpired => _connection.IsPasswordExpired;

		#endregion
		#region Exposed connection methods

		public void Open() => _connection.Open();
		public async void OpenAsync() => await _connection.OpenAsync();

		public void Close() => _connection.Close();
		public async void CloseAsync() => await _connection.CloseAsync();

		public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

		public bool Ping() => _connection.Ping();
		
		#endregion

		#region IDisposable Support

		private bool disposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					_connection.Dispose();
				}
				disposed = true;
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Raises an <see cref="ObjectDisposedException"/> if it has been disposed.
		/// </summary>
		/// <returns>The object. Useful for chaining functions.</returns>
		private TechlabMySQL RaiseIfDisposed()
		{
			if (disposed) throw new ObjectDisposedException(ToString());
			return this;
		}

		#endregion
	}
}