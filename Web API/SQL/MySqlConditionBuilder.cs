using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySQLWrapper.Data
{
	class MySqlConditionBuilder
	{
		/// <summary>
		/// Gets the full condition string. Fails if it is incomplete.
		/// </summary>
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
		/// <summary>
		/// A collection of <see cref="MySqlParameter"/> instances. These are created by <seealso cref="Operand(object, MySqlDbType)"/>.
		/// </summary>
		public List<MySqlParameter> Parameters { get; } = new List<MySqlParameter>();

		private string conditionString = "";
		private string verifiedString = ""; // To detect redundant calls to Verify()
		// Group (bracketed condition) depth
		private int depth = 0;
		private int cursor = 0;
		// Various flags to veryfy the order of function calls.
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
		/// Matches a specific column to a set of fields.
		/// </summary>
		/// <param name="column">The name of the column to match.</param>
		/// <param name="type">The type of the column to match.</param>
		/// <param name="fields">An array of values to compare to the column.</param>
		public MySqlConditionBuilder(string column, MySqlDbType type, params object[] fields)
		{
			NewGroup();
			foreach (var field in fields)
				Or().Column(column).Equals(field, type);
			EndGroup();
		}
		/// <summary>
		/// Auto-generates a condition that matches the primary key with the it's value in the
		/// given <see cref="SchemaItem"/>.
		/// </summary>
		/// <param name="item">The schemaItem whose primary key to use for the new condition.</param>
		public MySqlConditionBuilder(SchemaItem item)
		{
			Index PRIMARY = item.GetIndex("PRIMARY");
			if (PRIMARY == null)
				throw new ArgumentException("Item has no primary key.");
			var value = item.Fields[item.Indexes.IndexOf(PRIMARY)];
			Column(PRIMARY.Columns[0].Column);
			if (value == null) Is().Null();
			else Equals(value, PRIMARY.Columns[0].Type);
		}
		/// <summary>
		/// Auto-generates a condition that exactly matches the given fields and metadata.
		/// </summary>
		/// <param name="metadata">A set of <see cref="ColumnMetadata"/> objects.</param>
		/// <param name="fields">A set of objects to compare to the columns.</param>
		public MySqlConditionBuilder(ColumnMetadata[] metadata, object[] fields)
		{
			if (metadata.Length != fields.Length)
				throw new ArgumentException("Metadata and Fields must be equal in length.");
			for (int i = 0; i < fields.Length; i++)
			{
				// Append an AND
				if (i != 0) And();
				// Append conditions for each column
				if (fields[i] == null) Column(metadata[i].Column).Is().Null();
				else Column(metadata[i].Column).Equals(fields[i], metadata[i].Type);
			}
		}

		/// <summary>
		/// Appends the AND keyword to the condition. Appending this allows you to create a new condition.
		/// </summary>
		/// <returns></returns>
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
		/// <summary>
		/// Appends the OR keyword to the condition. Appending this allows you to create a new condition.
		/// </summary>
		/// <returns></returns>
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

		#region Operands
		/// <summary>
		/// Appends a column as operand to the condition. Fails if it is not expected.
		/// </summary>
		/// <remarks>
		/// Backquotes are automatically added.
		/// </remarks>
		/// <param name="name">The name of the column.</param>
		public MySqlConditionBuilder Column(string name)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Not expecting a condition operand.");
			try
			{ VerifyInput(name); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append column.", e); }
			Append($"`{name}`");
			expectingOperand = false;
			return this;
		}
		/// <summary>
		/// Appends a column as operand to the condition. Fails if it is not expected.
		/// </summary>
		/// <remarks>
		/// Backquotes are automatically added.
		/// </remarks>
		/// <param name="schema">The schema tho which the column belongs.</param>
		/// <param name="name">The name of the column.</param>
		public MySqlConditionBuilder Column(string schema, string name)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Not expecting a condition operand.");
			try
			{ VerifyInput(schema, name); }
			catch (Exception e)
			{ throw new OperationCanceledException("Can't append column.", e); }
			Append($"`{schema}`.`{name}`");
			expectingOperand = false;
			return this;
		}

		/// <summary>
		/// Appends a value to the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		/// <param name="type">The type of the value.</param>
		public MySqlConditionBuilder Operand(object value, MySqlDbType type)
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Can't append operand; Not expecting operand.");
			if (value != null)
			{
				var paramName = "@" + GetHashedName() + "_param" + Parameters.Count;
				Parameters.Add(new MySqlParameter(paramName, type) { Value = value });
				Append(paramName);
			}
			else
			{
				Null();
				return this;
			}
			expectingOperand = false;
			if (!modifyingOperand)
				unfinished = !unfinished;
			else modifyingOperand = false;
			return this;
		}
		/// <summary>
		/// Appends an NOT operand modifier the condition. Fails if it is not expected.
		/// </summary>
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
		/// <summary>
		/// Appends the TRUE keyword as operator. Effectively equivalent to 1 as integer.
		/// </summary>
		public MySqlConditionBuilder True()
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Can't append operand; Not expecting operand.");
			Append("TRUE");
			expectingOperand = false;
			if (!modifyingOperand)
				unfinished = !unfinished;
			else modifyingOperand = false;
			return this;
		}
		/// <summary>
		/// Appends the FALSE keyword as operator. Effectively equivalent to 0 as integer.
		/// </summary>
		public MySqlConditionBuilder False()
		{
			if (!expectingOperand)
				throw new OperationCanceledException("Can't append operand; Not expecting operand.");
			Append("FALSE");
			expectingOperand = false;
			if (!modifyingOperand)
				unfinished = !unfinished;
			else modifyingOperand = false;
			return this;
		}
		#endregion

		#region Comparison operators
		/// <summary>
		/// Appends an Equals operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder Equals() => AppendOperator('=');
		/// <summary>
		/// Appends an Equals operator and an operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		/// <param name="type">The type of the value.</param>
		public MySqlConditionBuilder Equals(object value, MySqlDbType type) => value != null ? Equals().Operand(value, type) : Is().Null();
		/// <summary>
		/// Appends an Is operator and an operand the condition. Fails if it is not expected.
		/// </summary>
		/// <remarks>
		/// 'IS' is the the only operator capable of checking if a value is NULL.
		/// </remarks>
		public MySqlConditionBuilder Is() => AppendOperator("IS");
		/// <summary>
		/// Appends an IS operator and an operand the condition. Fails if it is not expected.
		/// </summary>
		/// <remarks>
		/// 'IS' is the the only operator capable of checking if a value is NULL.
		/// </remarks>
		/// <param name="value">The value to append.</param>
		/// <param name="type">The type of the value.</param>
		public MySqlConditionBuilder Is(object value, MySqlDbType type) => Is().Operand(value, type);
		/// <summary>
		/// Appends a Not Equals operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder NotEquals() => AppendOperator("!=");
		/// <summary>
		/// Appends a Not Equals operator and an operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		/// <param name="type">The type of the value.</param>
		public MySqlConditionBuilder NotEquals(object value, MySqlDbType type) => value != null ? NotEquals().Operand(value, type) : Not().Is().Null();
		/// <summary>
		/// Appends a Less Than operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder LessThan() => AppendOperator('<');
		/// <summary>
		/// Appends a Less Than operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder LessThan(long value) => LessThan().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends a Less Than Or Equal operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder LessThanOrEqual() => AppendOperator("<=");
		/// <summary>
		/// Appends a Less Than Or Equal operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder LessThanOrEqual(long value) => LessThanOrEqual().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends a Greater Than operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder GreaterThan() => AppendOperator('>');
		/// <summary>
		/// Appends a Greater Than operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder GreaterThan(long value) => GreaterThan().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends a Greater Than Or Equal operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder GreaterThanOrEqual() => AppendOperator(">=");
		/// <summary>
		/// Appends a Greater Than Or Equal operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder GreaterThanOrEqual(long value) => GreaterThanOrEqual().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends the LIKE operator the condition. Fails if it is not expected.
		/// </summary>
		/// <remarks>
		/// See https://www.w3schools.com/sql/sql_like.asp for more info.
		/// </remarks>
		public MySqlConditionBuilder Like() => AppendOperator("LIKE");
		/// <summary>
		/// Appends the LIKE operator and a pattern operand the condition. Fails if it is not expected.
		/// </summary>
		/// <remarks>
		/// See https://www.w3schools.com/sql/sql_like.asp for more info.
		/// </remarks>
		/// <param name="pattern">A string pattern to compare a value to.</param>
		public MySqlConditionBuilder Like(string pattern) => Like().Operand(pattern, MySqlDbType.String);
		#endregion
		#region Arithmetic operators
		/// <summary>
		/// Appends the addition operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder Add() => AppendOperator('+');
		/// <summary>
		/// Appends the addition operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder Add(long value) => Add().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends the subtraction operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder Subtract() => AppendOperator('-');
		/// <summary>
		/// Appends the subtraction operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder Subtract(long value) => Subtract().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends the division operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder Divide() => AppendOperator('/');
		/// <summary>
		/// Appends the division operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder Divide(long value) => Divide().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends the multiplication operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder Multiply() => AppendOperator('*');
		/// <summary>
		/// Appends the division operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder Multiply(long value) => Multiply().Operand(value, MySqlDbType.Int64);
		/// <summary>
		/// Appends the modulo operator the condition. Fails if it is not expected.
		/// </summary>
		public MySqlConditionBuilder Modulo() => AppendOperator('%');
		/// <summary>
		/// Appends the modulo operator and a numeric operand the condition. Fails if it is not expected.
		/// </summary>
		/// <param name="value">The value to append.</param>
		public MySqlConditionBuilder Modulo(long value) => Modulo().Operand(value, MySqlDbType.Int64);
		#endregion

		#region Modifiers
		/// <summary>
		/// Appends the NOT modifier to the condition. Fails if it is not expected.
		/// </summary>
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
		#endregion

		#region Functions
		/// <summary>
		/// Appends the CURDATE() function that is equal to the current date.
		/// </summary>
		public MySqlConditionBuilder CurrentDate() => AppendOperator("CURDATE()");
		/// <summary>
		/// Appends the CURTIME() function that is equal to the current time.
		/// </summary>
		public MySqlConditionBuilder CurrentTime() => AppendOperator("CURTIME()");
		/// <summary>
		/// Appends the NOW() function that is equal to the current date and time.
		/// </summary>
		public MySqlConditionBuilder Now() => AppendOperator("NOW()");
		#endregion

		/// <summary>
		/// Appends a value as an operator to the condition.
		/// </summary>
		/// <param name="_operator">The operator value.</param>
		private MySqlConditionBuilder AppendOperator(object _operator)
		{
			if (expectingOperand)
				throw new OperationCanceledException($"Expecting operand, not operator '{_operator}'.");
			// Append the operator text to the condition with spaces.
			Append($" {_operator.ToString()} ");

			// Set regulatory flags
			modifyingOperand = false;
			expectingOperand = true;
			unfinished = true;

			// Return this object for method chaining
			return this;
		}

		/// <summary>
		/// Appends opening and closing brackets to the condition and enters them.
		/// </summary>
		/// <seealso cref="EndGroup"/>.
		public MySqlConditionBuilder NewGroup()
		{
			Append("()");
			cursor--;
			depth++;
			expectingOperand = true;
			unfinished = false;
			return this;
		}
		/// <summary>
		/// Exists a bracket group. Fails if none have been entered yet.
		/// </summary>
		public MySqlConditionBuilder EndGroup()
		{
			if (depth == 0)
				throw new OperationCanceledException("Can't exit main clause.");
			if (conditionString.Substring(0, cursor + 1).EndsWith("()"))
			{
				conditionString = conditionString.Substring(0, cursor - 1);
				cursor--;
			}
			else cursor++;
			depth--;
			return this;
		}

		/// <summary>
		/// Returns whether or not the condition is empty.
		/// </summary>
		public bool IsEmpty() => !conditionString.Any();

		/// <summary>
		/// Adds text at the cursor position and advances the cursor by the length of the text.
		/// </summary>
		/// <param name="txt">The text to append.</param>
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
			// To detect redundant calls
			verifiedString = conditionString;
		}

		/// <summary>
		/// Fully merges this conditionbuilder with a <see cref="MySqlCommand"/> by adding the condition string and parameters to the command.
		/// Fails if the condition is unfinished.
		/// </summary>
		/// <param name="cmd">The command object to merge with.</param>
		public void MergeCommand(MySqlCommand cmd)
		{
			MergeCommandText(cmd);
			MergeParameters(cmd);
		}
		/// <summary>
		/// Merges the condition string with a <see cref="MySqlCommand"/>'s commandText. Fails if the condition is unfinished.
		/// </summary>
		/// <param name="cmd">The command object to merge with.</param>
		public void MergeCommandText(MySqlCommand cmd)
		{
			Verify();
			if (!cmd.CommandText.TrimEnd().ToUpper().EndsWith("WHERE"))
				cmd.CommandText = cmd.CommandText.TrimEnd() + " WHERE";
			cmd.CommandText += ConditionString;
		}
		/// <summary>
		/// Adds the <see cref="Parameters"/> collection to a <see cref="MySqlCommand"/>.
		/// </summary>
		/// <param name="cmd">The command object to merge with.</param>
		public void MergeParameters(MySqlCommand cmd) => cmd.Parameters.AddRange(Parameters.ToArray());

		/// <summary>
		/// Sanitizes bad input text by throwing an exception. Otherwise, does noting.
		/// </summary>
		/// <param name="input">The input strings to parse.</param>
		private void VerifyInput(params string[] input)
		{
			foreach (var txt in input)
			{
				if (txt.Contains(';'))
					throw new OperationCanceledException("Use of semicolons is prohibited.");
			}
		}

		/// <summary>
		/// Convenience method for getting a relatively unique name for <see cref="MySqlParameter"/> objects.
		/// </summary>
		/// <returns>The concatination of the type name and object hashcode.</returns>
		private string GetHashedName() => GetType().Name + GetHashCode();
	}
}