using System;
using System.Drawing;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MySQLWrapper.MySQL
{
	static class SQLConstants
	{
		public const string SelectForeignKeys = "SELECT TABLE_NAME, COLUMN_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME " +
			"FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE REFERENCED_TABLE_SCHEMA = DATABASE() AND TABLE_NAME = <table>";

		public const string ConditionalSelect = "SELECT * FROM <table> WHERE <primary> = @condition";
		public const string Insert = "INSERT INTO <table> (<fields>) VALUES (<values>)";

		public const string SelectLastIndex = "SELECT LAST_INSERT_ID()";
	}
}
