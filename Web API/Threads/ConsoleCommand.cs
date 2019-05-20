using API.Commands;
using Logging;
using MySQLWrapper;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace API.Threads {
	class ConsoleCommand {
		public static void main(Logger log) {
			MethodInfo[] methods = typeof(CommandMethods).GetMethods();
			CommandMethods.wrapper = API.Program.createWrapper();

			log.Info("Thread ConsoleCommands now running.");
			while (true) {
				//Wait for input, then split it.
				string text = Console.ReadLine();
				string[] tokens = text.Split(" ");

				//Find the right command
				MethodInfo command = null;
				foreach (MethodInfo method in methods) {
					if (method.Name == tokens[0]) {
						command = method;
						break;
					}
				}

				//If no command was found, print an error and start over.
				if (command == null) {
					Console.WriteLine("Unknown Command");
					continue;
				}

				//Execute the 
				Object[] methodParams = new object[1] { tokens };
				command.Invoke(null, methodParams);
			}
		}
	}
}

namespace API.Commands {
	static partial class CommandMethods {
		public static TechlabMySQL wrapper;
	}
}
