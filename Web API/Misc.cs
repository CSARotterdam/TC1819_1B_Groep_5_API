using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace API {
	class Misc {
		public static MySqlConditionBuilder CreateCondition(JObject criteria, MySqlConditionBuilder condition = null) {
			condition = condition ?? new MySqlConditionBuilder();
			condition.NewGroup();
			int i = 0;
			foreach (KeyValuePair<string, JToken> pair in criteria) {
				if (i > 0) {
					condition.And();
				}

				condition.Column(pair.Key);
				string value = (string)pair.Value;
				string[] operands = value.Split("OR");
				foreach (string operand in operands) {
					string[] split = operand.Split(" ");
					if (split[0] == "LIKE") {
						condition.Like(split[1]);
					} else {
						condition.Equals(operand, MySql.Data.MySqlClient.MySqlDbType.String);
					}
					if (operands.Last() != operand) {
						condition.Or();
					}
				}
				i++;
			}
			return condition.EndGroup();
		}

		public static string FormatDelay(Stopwatch timer, int decimals = 0) {
			if (timer.ElapsedMilliseconds != 0)
				return Math.Round(timer.Elapsed.TotalMilliseconds, decimals) + " ms";
			if (timer.ElapsedTicks >= 10)
				return Math.Round(timer.ElapsedTicks / 10d, decimals) + " us";
			return Math.Round(timer.ElapsedTicks * 100d, decimals) + " ns";
		}

		public static Boolean verifyUsernameRegex(string username) {
			List<JObject> filters = Program.Settings["authenticationSettings"]["usernameRequirements"].ToObject<List<JObject>>();
			bool regexPass = true;
			foreach (JObject filter in filters) {
				filter.TryGetValue("regex", out JToken regex);
				regexPass = true;
				if (regex != null) {
					regexPass = false;
					string val = (string)regex;
					if (System.Text.RegularExpressions.Regex.IsMatch(username, val)) {
						regexPass = true;
					}
				}
				if (regexPass) {
					return true;
				};
			}
			return regexPass;
		}

		public static Boolean verifyUsernameLength(string username) {
			bool lengthPass = true;
			List<JObject> filters = Program.Settings["authenticationSettings"]["usernameRequirements"].ToObject<List<JObject>>();
			foreach (JObject filter in filters) {
				filter.TryGetValue("length", out JToken length);

				lengthPass = true;
				if (length != null) {
					lengthPass = false;
					int val = (int)length;
					if (username.Length == val) {
						lengthPass = true;
					}
				}
				if (lengthPass) {
					return true;
				};
			}
			return lengthPass;
		}
	}
}
