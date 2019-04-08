using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MySQLWrapper.Data
{
	class SqlConditionBuilder
	{
		public string ConditionString
		{
			get
			{
				try { Verify(); }
				catch (Exception e)
				{ throw new OperationCanceledException(); }
				return conditionString;
			}
			private set { conditionString = value; }
		}
		public List<MySqlParameter> Parameters { get; } = new List<MySqlParameter>();

		private string conditionString;
		private int depth = 0;
		private int cursor = 0;
		private bool unfinished = false;
		private bool expectingOperand = true;

		private bool newGroup
		{
			get
			{
				if ((depth != 0 && conditionString.Substring(0, cursor+1).EndsWith("()")) || !conditionString.Any()) return true;
				return false;
			}
		}

		public SqlConditionBuilder()
		{
			ConditionString = "";
		}

		public SqlConditionBuilder And()
		{
			if (newGroup) return this;
			try
			{ Verify(); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append AND statement.", e); }
			Append(" AND ");
			unfinished = true;
			expectingOperand = true;
			return this;
		}

		public SqlConditionBuilder Or()
		{
			if (newGroup) return this;
			try
			{ Verify(); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append OR statement.", e); }
			Append(" OR ");
			unfinished = true;
			expectingOperand = true;
			return this;
		}

		public SqlConditionBuilder Column(string txt)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Not expecting a condition operand.");
			try
			{ VerifyInput(txt); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append column.", e); }
			Append($"`{txt}`");
			expectingOperand = false;
			return this;
		}
		public SqlConditionBuilder Column(string schema, string txt)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Not expecting a condition operand.");
			try
			{ VerifyInput(schema, txt); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append column.", e); }
			Append($"`{schema}`.`{txt}`");
			expectingOperand = false;
			return this;
		}

		public SqlConditionBuilder Operand(object value, MySqlDbType type)
		{
			var paramName = "@" + GetHashedName() + "_param" + Parameters.Count;
			Parameters.Add(new MySqlParameter(paramName, type) { Value = value });
			Append(paramName);
			expectingOperand = false;
			unfinished = !unfinished;
			return this;
		}

		public SqlConditionBuilder Equals() => AppendOperator('=');
		public SqlConditionBuilder Equals(object value, MySqlDbType type) => AppendOperator('=').Operand(value, type);
		public SqlConditionBuilder LessThan() => AppendOperator('<');
		public SqlConditionBuilder LessThan(long value) => AppendOperator('<').Operand(value, MySqlDbType.Int64);

		private SqlConditionBuilder AppendOperator(char _operator)
		{
			if (expectingOperand)
				throw new OperationCanceledException("Expecting operand, not operator.");
			Append($" {_operator} ");
			expectingOperand = true;
			unfinished = true;
			return this;
		}

		public SqlConditionBuilder AndColumnEquals(string column, object condition, MySqlDbType type) => And().Column(column).Equals().Operand(condition, type);

		public SqlConditionBuilder NewGroup()
		{
			Append("()");
			cursor--;
			depth++;
			expectingOperand = true;
			unfinished = false;
			return this;
		}
		public SqlConditionBuilder ExitGroup()
		{
			if (depth == 0)
				throw new OperationCanceledException("Can't exit main clause.");
			if (conditionString.Substring(0, cursor+1).EndsWith("()"))
				throw new OperationCanceledException("Can't exit empty group.");
			cursor++;
			depth--;
			return this;
		}

		private void Append(string txt)
		{
			conditionString = conditionString.Insert(cursor, txt);
			cursor += txt.Length;
		}

		private void Verify()
		{
			//var currentCondition = conditionString.Substring(0, cursor);
			if (unfinished) throw new FormatException("Not all conditions are completed.");
			if (depth != 0 && conditionString.Substring(0, cursor+1).EndsWith("()"))
				throw new FormatException("Not all groups are filled with a condition.");
		}

		private void VerifyInput(params string[] input)
		{
			foreach (var txt in input)
			{
				if (txt.Contains(';'))
					throw new OperationCanceledException("Use of semicolons is prohibited.");
			}
		}

		private string GetHashedName() => GetType().Name + GetHashCode();
	}
}
