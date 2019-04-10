using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MySQLWrapper.Data
{
	class MySqlConditionBuilder
	{
		public string ConditionString
		{
			get
			{
				try
				{ Verify(); }
				catch (Exception e)
				{ throw new OperationCanceledException("Condition verification failed.", e); }
				return conditionString;
			}
			private set { conditionString = value; }
		}
		public List<MySqlParameter> Parameters { get; } = new List<MySqlParameter>();

		private string conditionString = "";
		private int depth = 0;
		private int cursor = 0;
		private bool unfinished = false;
		private bool expectingOperand = true;
		private bool modifyingOperand = false;
		private List<(string Schema, string Column)> columns = new List<(string Schema, string Column)>();

		private bool newGroup
		{
			get
			{
				if ((depth != 0 && conditionString.Substring(0, cursor+1).EndsWith("()")) || conditionString.Length == 0) return true;
				return false;
			}
		}

		public MySqlConditionBuilder And()
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

		public MySqlConditionBuilder Or()
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

		public MySqlConditionBuilder Column(string txt)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Not expecting a condition operand.");
			try
			{ VerifyInput(txt); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append column.", e); }
			Append($"`{txt}`");
			columns.Add((null, txt));
			expectingOperand = false;
			return this;
		}
		public MySqlConditionBuilder Column(string schema, string txt)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Not expecting a condition operand.");
			try
			{ VerifyInput(schema, txt); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append column.", e); }
			Append($"`{schema}`.`{txt}`");
			columns.Add((schema, txt));
			expectingOperand = false;
			return this;
		}

		public MySqlConditionBuilder Operand(object value, MySqlDbType type)
		{
			var paramName = "@" + GetHashedName() + "_param" + Parameters.Count;
			Parameters.Add(new MySqlParameter(paramName, type) { Value = value });
			Append(paramName);
			expectingOperand = false;
			if (!modifyingOperand)
				unfinished = !unfinished;
			else modifyingOperand = false;
			return this;
		}

		public MySqlConditionBuilder Equals() => AppendOperator('=');
		public MySqlConditionBuilder Equals(object value, MySqlDbType type) => Equals().Operand(value, type);
		public MySqlConditionBuilder NotEquals() => AppendOperator("!=");
		public MySqlConditionBuilder NotEquals(object value, MySqlDbType type) => NotEquals().Operand(value, type);
		public MySqlConditionBuilder LessThan() => AppendOperator('<');
		public MySqlConditionBuilder LessThan(long value) => LessThan().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder LessThanOrEqual() => AppendOperator("<=");
		public MySqlConditionBuilder LessThanOrEqual(long value) => LessThanOrEqual().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder GreaterThan() => AppendOperator('>');
		public MySqlConditionBuilder GreaterThan(long value) => GreaterThan().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder GreaterThanOrEqual() => AppendOperator(">=");
		public MySqlConditionBuilder GreaterThanOrEqual(long value) => GreaterThanOrEqual().Operand(value, MySqlDbType.Int64);

		public MySqlConditionBuilder Add() => AppendOperator('+');
		public MySqlConditionBuilder Add(long value) => Add().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder Subtract() => AppendOperator('-');
		public MySqlConditionBuilder Subtract(long value) => Subtract().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder Divide() => AppendOperator('/');
		public MySqlConditionBuilder Divide(long value) => Divide().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder Multiply() => AppendOperator('*');
		public MySqlConditionBuilder Multiply(long value) => Multiply().Operand(value, MySqlDbType.Int64);
		public MySqlConditionBuilder Modulo() => AppendOperator('%');
		public MySqlConditionBuilder Modulo(long value) => Modulo().Operand(value, MySqlDbType.Int64);

		public MySqlConditionBuilder Not()
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Can't append NOT: Not expecting operand.");
			if (newGroup) Append("NOT ");
			else Append(" NOT ");
			modifyingOperand = true;
			unfinished = true;
			return this;
		}

		private MySqlConditionBuilder AppendOperator(object _operator)
		{
			if (expectingOperand)
				throw new OperationCanceledException($"Can't append operator '{_operator}': Expecting operand, not operator.");
			Append($" {_operator.ToString()} ");
			expectingOperand = true;
			unfinished = true;
			return this;
		}

		public MySqlConditionBuilder ColumnEquals(string column, object condition, MySqlDbType type) => Column(column).Equals().Operand(condition, type);

		public MySqlConditionBuilder NewGroup()
		{
			Append("()");
			cursor--;
			depth++;
			expectingOperand = true;
			unfinished = false;
			return this;
		}
		public MySqlConditionBuilder ExitGroup()
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
			if (unfinished) throw new FormatException("Not all conditions are completed.");
			if (depth != 0 && conditionString.Substring(0, cursor+1).EndsWith("()"))
				throw new FormatException("Not all groups are filled with a condition.");
		}

		public void Verify(string Schema, ColumnMetadata[] columns)
		{
			throw new NotImplementedException(); // TODO: Implement column verification.
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
