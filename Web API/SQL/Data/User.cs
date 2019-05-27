using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MySQLWrapper.Data
{
	enum UserPermission
	{
		Empty,
		User,
		Collaborator,
		Admin
	}

	sealed class User : SchemaItem
	{
		#region Schema Metadata
		public const string schema = "users";
		public static readonly ReadOnlyCollection<ColumnMetadata> metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("username", 50, MySqlDbType.VarChar),
			new ColumnMetadata("password", char.MaxValue, MySqlDbType.Text),
			new ColumnMetadata("permissions", 3, MySqlDbType.Enum),
			new ColumnMetadata("token", 20, MySqlDbType.Int64),
 		});
		public static readonly ReadOnlyCollection<Index> indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, metadata[0])
		});
		private readonly object[] _fields = new object[metadata.Count];
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
}