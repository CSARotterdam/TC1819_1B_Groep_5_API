namespace MySQLWrapper.MySQL
{
	static class SQLConstants
	{
		public const string Select = "SELECT <columns> FROM <schema> WHERE <condition>";
		public const string Insert = "INSERT INTO <schema> (<columns>) VALUES (<values>)";
		public const string Update = "UPDATE <schema> SET <column_value_pairs> WHERE <condition>";
		public const string Delete = "DELETE FROM <schema> WHERE <condition>";

		/// <summary>
		/// Builds and return a SELECT sql query.
		/// </summary>
		public static string GetSelect(string columns, string schema, string condition) =>
			Select.Replace("<condition>", condition).Replace("<schema>", schema).Replace("<columns>", columns);
		/// <summary>
		/// Builds and return a SELECT sql query.
		/// </summary>
		public static string GetSelect(string columns, string schema, string condition, long limit) => GetSelect(columns, schema, condition) + " LIMIT " + limit;
		/// <summary>
		/// Builds and return a INSERT sql command.
		/// </summary>
		public static string GetInsert(string schema, string columns, string values) =>
			Insert.Replace("<values>", values).Replace("<columns>", columns).Replace("<schema>", schema);
		/// <summary>
		/// Builds and return a UPDATE sql command.
		/// </summary>
		public static string GetUpdate(string schema, string column_value_pairs, string condition) =>
			Update.Replace("<condition>", condition).Replace("<column_value_pairs>", column_value_pairs).Replace("<schema>", schema);
		/// <summary>
		/// Builds and return a DELETE sql command.
		/// </summary>
		public static string GetDelete(string schema, string condition) =>
			Delete.Replace("<condition>", condition).Replace("<schema>", schema);
	}
}
