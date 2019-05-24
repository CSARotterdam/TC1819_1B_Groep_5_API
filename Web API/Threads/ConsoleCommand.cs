using API.Commands;
using API.Requests;
using Logging;
using MySQLWrapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace API.Threads {
	class ConsoleCommand {
		public static void main(Logger log) {
			MethodInfo[] methods = typeof(CommandMethods).GetMethods();
			CommandMethods.wrapper = API.Program.CreateWrapper();
			CommandMethods.log = log;

			log.Info("Thread ConsoleCommands now running.");
			while (true) {
				//Wait for input, then split it.
				string text = Console.ReadLine();
				string[] tokens = text.Split(" ");

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
				try
				{ command.Invoke(null, new object[] { tokens.TakeLast(tokens.Length - 1).ToArray() }); }
				catch (Exception e)
				{ log.Error($"{e.GetType().Name}: {e.Message}", e, true); }
			}
		}
	}
}

namespace API.Commands {
	static partial class CommandMethods {
		public static TechlabMySQL wrapper;
		public static Logger log;
		public static Stopwatch timer = new Stopwatch();

		public static void Ping(string[] args)
		{
			List<double> delays = new List<double>();
			int pingCount = 4;
			try { pingCount = int.Parse(args[0]); }
			catch (Exception) { }
			for (int i = 0; i < pingCount; i++)
			{
				if (i != 0) Thread.Sleep(1000 - (int)delays.Last());
				timer.Start();
				bool status = wrapper.Ping();
				timer.Stop();
				if (status) log.Info($"Reply after {Misc.FormatDelay(timer, 1)}");
				else
				{
					log.Info($"Failed after {Misc.FormatDelay(timer, 1)}");
					timer.Reset();
					break;
				}
				delays.Add(timer.Elapsed.TotalMilliseconds);
				timer.Reset();
			}
			if (delays.Count > 1) log.Info($"Average: {Math.Round(delays.Average(), 2)} ms");
		}

		public static void ReloadConfig(string[] args)
		{
			lock (Program.Settings)
				Program.Settings = Config.loadConfig();
			log.Info("Successfully reloaded config");
		}
	}
}
