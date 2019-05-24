using Logging;
using MySQLWrapper;
using Newtonsoft.Json;
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
		public static dynamic Settings;

		public static void Main() {
			// Compress previous log
			if (File.Exists("Logs/latest.log")) {
				var lastArchive = Directory.GetFiles("Logs").OrderBy(x => File.GetCreationTime(x)).LastOrDefault();
				if (lastArchive != null)
				{
					using (var archive = ZipFile.Open(lastArchive, ZipArchiveMode.Update))
						archive.CreateEntryFromFile("Logs/latest.log", "latest.log");
					File.Delete("Logs/latest.log");
				}
			}

			log.OutputStreams.Add(new AdvancingWriter("Logs/latest.log") {
				Compression = true,
				Archive = "Logs/{0:dd-MM-yyyy}.{2}.zip"
			});

			log.Info("Server is starting!");

			//Start logger
			Requests.RequestMethods.log = log;

			//Load configuration file
			log.Info("Loading configuration file.");
			bool validConfig = true;

			try {
				Settings = Config.loadConfig();
			} catch (JsonReaderException) {
				log.Fatal("Configuration file is not a valid JSON file.");
				log.Fatal("Validate the file at https://www.jsonschemavalidator.net/");
				log.Fatal("Press the any key to exit.");
				Console.ReadKey();
				return;
			}

			//Check if config contains all necessary info to start. If it doesn't, abort launch.
			if (Settings.databaseSettings.username == null || Settings.databaseSettings.password == null || Settings.databaseSettings.serverAddress == null || Settings.databaseSettings.database == null) {
				log.Fatal("Incomplete or missing database configuration.");
				validConfig = false;
			}
			if ((bool)Settings.connectionSettings.autodetect && Settings.connectionSettings.serverAddresses.Count == 0) {
				log.Fatal("Missing server address.");
				validConfig = false;
			}

			//If the config file is invalid, throw an error and abort.
			if (!validConfig) {
				log.Fatal("\n");
				log.Fatal("The server failed to start because of an invalid configuration setting. Please check the server configuration!");
				log.Fatal("Press the any key to exit.");
				Console.ReadKey();
				return;
			}

			if ((string)Settings["authenticationSettings"]["expiration"] == null) {
				log.Info("User token expiration not set. Defaulting to 7200 (2 hours).");
				Settings["authenticationSettings"]["expiration"] = 7200;
			}

			log.Info("Successfully loaded configuration file.");

			//Get local IP address, if autodetect is enabled in settings.
			List<string> addresses = Settings.connectionSettings.serverAddresses.ToObject<List<string>>();
			if ((bool)Settings.connectionSettings.autodetect) {
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
			BlockingCollection<HttpListenerContext> requestQueue = new BlockingCollection<HttpListenerContext>();

			//Create console command thread
			Thread consoleThread = new Thread(() => API.Threads.ConsoleCommand.main(log));
			consoleThread.Start();

			//Create worker threads
			log.Info("Creating worker threads.");
			int threadCount = (int)Settings.performanceSettings.workerThreadCount;
			Thread[] threadList = new Thread[threadCount];
			for (int i = 0; i != threadCount; i++) {
				Thread workerThread = new Thread(() => API.Threads.RequestWorker.Main(log, requestQueue)) {
					Name = "WorkerThread" + i.ToString()
				};
				workerThread.Start();
				threadList[i] = workerThread;
			}
			log.Info("Finished creating worker threads.");

			// Create listener thingy.
			HttpListener listener = new HttpListener();
			foreach (string address in addresses) {
				listener.Prefixes.Add("http://" + address + "/");
			}
			Thread ListenerThread = new Thread(() => API.Threads.Listener.main(log, listener, requestQueue)) {
				Name = "ListenerThread"
			};
			ListenerThread.Start();
			log.Info("Setup complete.");

			// Wait until all threads are terminated
			consoleThread.Join();
			ListenerThread.Join();
			foreach (var t in threadList) {
				t.Join();
			}

			// Exit main thread
			log.Dispose();
		}

		public static TechlabMySQL CreateWrapper() {
			string databaseAddress = Program.Settings.databaseSettings.serverAddress;
			string databasePort;
			string[] splitAddress = databaseAddress.Split(":");
			if (databaseAddress == splitAddress[0]) {
				databasePort = "3306";
			} else {
				databaseAddress = splitAddress[0];
				databasePort = splitAddress[1];
			}
			TechlabMySQL wrapper = new TechlabMySQL( //TODO: Catch access denied, other exceptions.
				databaseAddress,
				databasePort,
				(string)Program.Settings.databaseSettings.username,
				(string)Program.Settings.databaseSettings.password,
				(string)Program.Settings.databaseSettings.database,
				(int)Program.Settings.databaseSettings.connectionTimeout,
				(bool)Program.Settings.databaseSettings.persistLogin
			) {
				AutoReconnect = true
			};
			Requests.RequestMethods.wrapper = wrapper;
			wrapper.Open();
			return wrapper;
		}
	}
}
