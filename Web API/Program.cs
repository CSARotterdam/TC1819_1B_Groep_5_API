using API.Threads;
using Logging;
using MySql.Data.MySqlClient;
using MySQLWrapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace API {
	class Program {
		public static bool ManualError = false;

		public static Logger log = new Logger(Level.ALL, Console.Out);
		public static TechlabMySQL Connection;

		public static RequestWorker[] RequestWorkers;
		public static Listener ListenerThread;
		public static Thread ConsoleThread;

		public static JObject Settings;

		private static string LogDir
		{
			get
			{
				if (Settings == null || !Settings.ContainsKey("logSettings") || !((JObject)Settings["logSettings"]).ContainsKey("outputDir"))
					return "Logs";
				return (string)Settings["logSettings"]["outputDir"];
			}
		}
		private static string Logs(string file) => Path.Combine(LogDir, file);

		public static void Main() {
			log.Info("Server is starting!");

			//Load configuration file
			log.Config("Loading configuration file");
			Settings = Config.loadConfig();
			if (Settings == null) {
				log.Fatal("The server failed to start because of an invalid configuration setting. Please check the server configuration!");
				log.Fatal("Press the any key to exit...");
				Console.ReadKey();
				Console.WriteLine();
				log.Close();
				return;
			}

			if (Settings.ContainsKey("logSettings"))
			{
				JObject logSettings = (JObject)Settings["logSettings"];
				if (logSettings.ContainsKey("logLevel"))
				{
					var logLevel = (Level)typeof(Level).GetFields().First(x => x.Name.ToLower() == ((string)logSettings["logLevel"]).ToLower()).GetValue(null);
					if (logLevel != null)
						log.LogLevel = logLevel;
				}
				if (logSettings.ContainsKey("format"))
					log.Format = (string)logSettings["format"];
			}

			log.Config("Setting up files and directories...");
			try {
				IOSetup();
			} catch (IOException e ) {
				log.Fatal("Failed to set up files and directories.");
				log.Fatal(e.Message);
				Console.ReadKey();
				Console.WriteLine();
				log.Close();
				return;
			}
			
			log.OutputStreams.Add(new AdvancingWriter(Logs("latest.log"))
			{
				Compression = true,
				CompressOnClose = false,
				Archive = Logs("{0:dd-MM-yyyy}.{2}.zip")
			});


			//Get local IP address, if autodetect is enabled in settings.
			List<string> addresses = Settings["connectionSettings"]["serverAddresses"].ToObject<List<string>>();
			if ((bool)Settings["connectionSettings"]["autodetect"]) {
				string address;
				using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
					socket.Connect("8.8.8.8", 65530);
					IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
					address = endPoint.Address.ToString();
					addresses.Add(address);
				}
				log.Info("Detected IPv4 address to be " + address);
			}

			//Create request queue
			var requestQueue = new BlockingCollection<HttpListenerContext>();

			// Create listener thingy.
			HttpListener listener = new HttpListener();
			foreach (string address in addresses)
				listener.Prefixes.Add("http://" + address + "/");
			ListenerThread = new Listener(listener, requestQueue, "ListenerThread", log);
			try {
				ListenerThread.Start();
			} catch (HttpListenerException e) {
				log.Fatal("Failed to create Listener. Are you running the server in Administrator mode?.");
				log.Fatal(e.Message);
				Console.ReadKey();
				Console.WriteLine();
				log.Close();
				return;
			}

			//Create console command thread
			ConsoleThread = new Thread(() => API.Threads.ConsoleCommand.main(log)) {
				IsBackground = true
			};
			ConsoleThread.Start();

			//Create worker threads
			log.Config("Creating worker threads...");
			int threadCount = (int)Settings["performanceSettings"]["workerThreadCount"];
			RequestWorkers = new RequestWorker[threadCount];
			for (int i = 0; i < threadCount; i++) {
				try {
					var worker = new RequestWorker(CreateConnection(), requestQueue, "RequestWorker" + (i + 1), log);
					worker.Start();
					RequestWorkers[i] = worker;
				} catch(MySqlException e) {
					log.Error("Failed to create connection for RequestWorker" + (i + 1));
					log.Error(e.Message);
				}
			}
			
			log.Info("Finished setup");

			// Wait until all threads are terminated
			foreach (var worker in RequestWorkers.Where(x => x != null)) {
				worker.Join();
				log.Fine($"Stopped '{worker}'");
			}

			// Exit main thread
			log.Info("Exiting program...");
			log.Close();
		}

		public static TechlabMySQL CreateConnection() {
			string databaseAddress = (string)Settings["databaseSettings"]["serverAddress"];
			string databasePort;
			string[] splitAddress = databaseAddress.Split(":");
			if (databaseAddress == splitAddress[0]) {
				databasePort = "3306";
			} else {
				databaseAddress = splitAddress[0];
				databasePort = splitAddress[1];
			}
			TechlabMySQL wrapper = new TechlabMySQL(
				databaseAddress,
				databasePort,
				(string)Settings["databaseSettings"]["username"],
				(string)Settings["databaseSettings"]["password"],
				(string)Settings["databaseSettings"]["database"],
				(int)Settings["databaseSettings"]["connectionTimeout"],
				(bool)Settings["databaseSettings"]["persistLogin"],
				(bool)Settings["databaseSettings"]["caching"]
			) {
				AutoReconnect = true
			};
			Connection = wrapper;
			wrapper.Open();
			return wrapper;
		}

		/// <summary>
		/// Performs setup for all IO-related features
		/// </summary>
		public static void IOSetup() {
			// Create folders
			Directory.CreateDirectory(LogDir);

			// Compress previous log
			var logfile = Logs("latest.log");
			if (File.Exists(logfile))
			{
				// Get most recent zip file
				var lastArchive = Directory.GetFiles(LogDir)
					.OrderBy(x => File.GetCreationTime(x))
					.Where(x => Path.GetExtension(x).ToLower() == ".zip")
						.LastOrDefault();
				// Add latest.log to the most recent archive if it exists
				if (lastArchive != null)
				{
					using (var archive = ZipFile.Open(lastArchive, ZipArchiveMode.Update))
						archive.CreateEntryFromFile(logfile, Path.GetFileName(logfile));
					File.Delete(logfile);
				}
			}
		}
	}
}
