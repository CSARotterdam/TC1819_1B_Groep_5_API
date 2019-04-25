using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

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
				if (conditionString.Length == 0) return "TRUE";
				return conditionString;
			}
		}
		public List<MySqlParameter> Parameters { get; } = new List<MySqlParameter>();

		private string conditionString = "";
		private string verifiedString = "";
		private int depth = 0;
		private int cursor = 0;
		private bool unfinished = false;
		private bool expectingOperand = true;
		private bool modifyingOperand = false;

		private bool newGroup
		{
			get
			{
				if ((depth != 0 && conditionString.Substring(0, cursor+1).EndsWith("()")) || conditionString.Length == 0) return true;
				return false;
			}
		}

		/// <summary>
		/// Creates a new instance of <see cref="MySqlConditionBuilder"/>.
		/// </summary>
		public MySqlConditionBuilder() { }
		/// <summary>
		/// Auto-generates a condition that exactly matches the given fields and metadata.
		/// </summary>
		/// <param name="Metadata">A set of <see cref="ColumnMetadata"/> objects.</param>
		/// <param name="Fields">A set of objects to compare to the columns.</param>
		public MySqlConditionBuilder(ColumnMetadata[] Metadata, object[] Fields)
		{
			if (Metadata.Length != Fields.Length)
				throw new ArgumentException("Metadata and Fields must be equal in length.");
			for (int i = 0; i < Fields.Length; i++)
			{
				if (i != 0) And();
				if (Fields[i] == null) Column(Metadata[i].Column).Equals().Null();
				else ColumnEquals(Metadata[i].Column, Fields[i], Metadata[i].Type);
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
			expectingOperand = false;
			return this;
		}

		public MySqlConditionBuilder Operand(object value, MySqlDbType type)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Can't append operand; Not expecting operand.");
			var paramName = "@" + GetHashedName() + "_param" + Parameters.Count;
			Parameters.Add(new MySqlParameter(paramName, type) { Value = value });
			Append(paramName);
			expectingOperand = false;
			if (!modifyingOperand)
				unfinished = !unfinished;
			else modifyingOperand = false;
			return this;
		}
		public MySqlConditionBuilder Null()
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Can't append operand; Not expecting operand.");
			Append("NULL");
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
		public MySqlConditionBuilder Like() => AppendOperator("LIKE");
		public MySqlConditionBuilder Like(object value, MySqlDbType type) => Like().Operand(value, type);

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

		/// <summary>
		/// Checks if the condition is valid. This detects and skip redundant calls.
		/// </summary>
		/// <exception cref="FormatException">Thrown when the condition is not valid.</exception>
		public void Verify()
		{
			if (verifiedString == conditionString) return;
			if (unfinished) throw new FormatException("Not all conditions are completed.");
			if (depth != 0 && conditionString.Substring(0, cursor+1).EndsWith("()"))
				throw new FormatException("Not all groups are filled with a condition.");
			verifiedString = conditionString;
		}

		public void MergeCommand(MySqlCommand cmd)
		{
			MergeCommandText(cmd);
			MergeParameters(cmd);
		}
		public void MergeCommandText(MySqlCommand cmd)
		{
			Verify();
			if (!cmd.CommandText.TrimEnd().ToUpper().EndsWith("WHERE"))
				cmd.CommandText = cmd.CommandText.TrimEnd() + " WHERE";
			cmd.CommandText += ConditionString;
		}
		public void MergeParameters(MySqlCommand cmd) => cmd.Parameters.AddRange(Parameters.ToArray());

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