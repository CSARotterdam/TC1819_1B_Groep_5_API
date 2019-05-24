using API.Commands;
using Logging;
using MySQLWrapper;
using MySQLWrapper.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
				string[] tokens = Regex.Matches(text, @"[\""].+?[\""]|[^ ]+")
						.Cast<Match>()
						.Select(m => m.Value.Trim('"'))
						.ToArray();

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
				{
					CommandMethods.timer.Reset();
					log.Error($"{e.InnerException.GetType().Name}: {e.InnerException.Message}", false);
				}
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

		public static void UploadImage(params string[] args)
		{
			if (args.Length < 1)
			{
				log.Error("UploadImage requires one argument");
				return;
			}
			if (!File.Exists(args[0]))
			{
				log.Error($"{args[0]} is not a valid file path");
				return;
			}
			var image = new Image(args[0]);
			timer.Start();
			wrapper.Upload(image);
			timer.Stop();
			log.Info($"({Misc.FormatDelay(timer)}) Uploaded {Path.GetFileName(args[0])}");
			timer.Reset();
		}

		public static void UploadImages(params string[] args)
		{
			if (args.Length < 1)
			{
				log.Error("UploadImages requires one argument");
				return;
			}
			if (!Directory.Exists(args[0]))
			{
				log.Error($"{args[0]} is not a valid directory");
				return;
			}
			var files = Directory.GetFiles(args[0]);
			foreach (var file in files)
				if (Image.ImageFormats.Contains(Path.GetExtension(file)))
					UploadImage(file);
		}
	}
}
