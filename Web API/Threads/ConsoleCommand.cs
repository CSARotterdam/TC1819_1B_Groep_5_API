using API.Commands;
using Logging;
using MySQLWrapper;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace API.Threads {
	class ConsoleCommand {
		public static void main(Logger log) {
			MethodInfo[] methods = typeof(CommandMethods).GetMethods();
			CommandMethods.Connection = API.Program.CreateConnection();
			CommandMethods.log = log;

			log.Config("Starting thread 'ConsoleCommands'");
			while (true) {
				//Wait for input, then split it.
				string text = Console.ReadLine();
				string[] tokens = Regex.Matches(text, @"[\""].+?[\""]|[^ ]+")
						.Cast<Match>()
						.Select(m => m.Value.Trim('"'))
						.ToArray();
				if (tokens.Length == 0) continue;

				//Find the right command
				MethodInfo command = null;
				foreach (MethodInfo method in methods) {
					if (method.Name.ToLower() == tokens[0].ToLower()) {
						command = method;
						break;
					}
				}

				//If no command was found, print an error and start over.
				if (command == null) {
					log.Info("Unknown Command");
					continue;
				}

				//Execute the command
				try { command.Invoke(null, new object[] { tokens.TakeLast(tokens.Length - 1).ToArray() }); } catch (Exception e) {
					CommandMethods.timer.Reset();
					log.Error($"{e.InnerException.GetType().Name}: {e.InnerException.Message}", e.InnerException, false);
				}
			}
		}
	}
}

namespace API.Commands {
	static partial class CommandMethods {
		public static TechlabMySQL Connection;
		public static Logger log;
		public static Stopwatch timer = new Stopwatch();
	}
}
