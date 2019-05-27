using MySql.Data.MySqlClient;
using MySQLWrapper.MySQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySQLWrapper.Data
{
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
				if (AutoIncrement != null)
				{
					object o;
					switch (AutoIncrement.Columns[0].Type)
					{
						// Convert type because for some reason casting a long that is an object to int is invalid, but casting a regular long is fine???
						case MySqlDbType.Byte: o = (sbyte)scalar; break;
						case MySqlDbType.UByte: o = (byte)scalar; break;
						case MySqlDbType.Int16: o = (short)scalar; break;
						case MySqlDbType.UInt16: o = (ushort)scalar; break;
						case MySqlDbType.Int24:
						case MySqlDbType.Int32: o = (int)scalar; break;
						case MySqlDbType.UInt24:
						case MySqlDbType.UInt32: o = (uint)scalar; break;
						default:
						case MySqlDbType.Int64: o = scalar; break;
						case MySqlDbType.UInt64: o = (ulong)scalar; break;
					}
					Fields[Metadata.IndexOf(AutoIncrement.Columns[0])] = o;
				}

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
				var condition = GetIndex("PRIMARY") != null
					? new MySqlConditionBuilder(this)
					: new MySqlConditionBuilder(Metadata.ToArray(), Fields);
				cmd.CommandText = SQLConstants.GetDelete(
					Schema,
					condition.ConditionString
				);
				condition.MergeParameters(cmd);

				var cmdOut = cmd.ExecuteNonQuery();
				ClearTrace();
				return cmdOut;
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
                string addValues(ColumnMetadata meta, object value) {
                    if (value == null) return $"`{meta.Column}` = NULL";
                    var paramName = $"@param{cmd.Parameters.Count}";
                    cmd.Parameters.Add(new MySqlParameter(paramName, meta.Type) { Value = value });
                    return $"`{meta.Column}` = {paramName}";
                }
                var columnValuePairs = string.Join(", ", Metadata.Zip(Fields, (x, y) => addValues(x, y)));
				var condition = GetIndex("PRIMARY") != null
					? new MySqlConditionBuilder(this)
					: new MySqlConditionBuilder(Metadata.ToArray(), Fields);
				cmd.CommandText = SQLConstants.GetUpdate(
					Schema,
					columnValuePairs,
					condition.ConditionString
				);
				condition.MergeParameters(cmd);

				var cmdOut = cmd.ExecuteNonQuery();
				UpdateTrace();
				return cmdOut;
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
		public override string ToString() => GetType().Name + "(" + string.Join(", ", Metadata.Zip(Fields, (x, y) => $"{x.Column}: {y ?? "NULL"}")) + ")";
	}
}