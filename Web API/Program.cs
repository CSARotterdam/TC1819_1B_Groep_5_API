using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MySQLWrapper;
using Logging;

namespace API {
	class Program {
		public static TechlabMySQL wrapper;
		public static bool ManualError = false;
		public static Logger log = new Logger(Level.ALL, Console.Out);
		private static int _errorCode;
		public static int ErrorCode{
			get { return _errorCode; }
			set {
				_errorCode = value;
				log.Error("Error code changed to " + _errorCode.ToString());
				if(_errorCode == 0){
					log.Error("Error state disabled. Now accepting requests.");
				} else {
					log.Error("Error state enabled. All requests will be refused.");
				}
			}
		}

		public static void Main() {
			log.Info("Hello world!");

			//Load configuration file
			dynamic Settings = Config.loadConfig();

			//Check if config contains all necessary info to start. If it doesn't, abort launch.
			bool validConfig = true;
			if(Settings.databaseSettings.username == null || Settings.databaseSettings.password == null || Settings.databaseSettings.serverAddress == null || Settings.databaseSettings.database == null) {
				log.Error("Error: Incomplete database configuration.");
				validConfig = false;
			}
			if((bool)Settings.connectionSettings.autodetect && Settings.connectionSettings.serverAddresses.Count == 0) {
				log.Error("Error: Missing server address.");
				validConfig = false;
			}
			if (!validConfig) {
				log.Error("The server failed to start because there is at least one invalid setting. Please check the server configuration!");
				log.Error("Press the any key to exit.");
				Console.ReadLine();
				return;
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

			//Connect to database
			string databaseAddress = Settings.databaseSettings.serverAddress;
			string databasePort = "";
			string[] splitAddress = databaseAddress.Split(":");
			if(databaseAddress == splitAddress[0]){
				databasePort = "3306";
			} else {
				databaseAddress = splitAddress[0];
				databasePort = splitAddress[1];
			}
			wrapper = new TechlabMySQL( //TODO: Catch access denied, other exceptions.
				databaseAddress,
				databasePort,
				(string)Settings.databaseSettings.username,
				(string)Settings.databaseSettings.password,
				(string)Settings.databaseSettings.database,
				(int)Settings.databaseSettings.connectionTimeout,
				(bool)Settings.databaseSettings.persistLogin
			);
			wrapper.Open();

			//Create database maintainer thread
			Thread databaseMaintainerThread = new Thread(() => API.Threads.DatabaseMaintainer.main());
			databaseMaintainerThread.Start();

			//Create worker threads
			log.Info("Creating worker threads.");
			int threadCount = (int)Settings.performanceSettings.workerThreadCount;
			Thread[] threadList = new Thread[threadCount];
			for (int i = 0; i != threadCount; i++) {
				Thread workerThread = new Thread(() => API.Threads.RequestWorker.main(log, requestQueue)) {
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
			Thread ListenerThread = new Thread(() => API.Listener.main(log, listener, requestQueue)) {
				Name = "ListenerThread"
			};
			ListenerThread.Start();
			log.Info("Setup complete.");
		}
	}
}
