﻿using API.Threads;
using Logging;
using MySQLWrapper;
using Newtonsoft.Json;
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

		public static List<RequestWorker> RequestWorkers = new List<RequestWorker>();
		public static Listener ListenerThread;
		public static Thread ConsoleThread;

		public static JObject Settings;

		public static void Main() {
			IOSetup();

			log.OutputStreams.Add(new AdvancingWriter("Logs/latest.log", new TimeSpan(0, 0, 5)) {
				Compression = true,
				CompressOnClose = false,
				Archive = "Logs/{0:dd-MM-yyyy}.{2}.zip"
			});

			log.Info("Server is starting!");

			//Load configuration file
			log.Info("Loading configuration file.");
			Settings = Config.loadConfig();
			if(Settings == null) {
				log.Fatal("The server failed to start because of an invalid configuration setting. Please check the server configuration!");
				log.Fatal("Press the any key to exit...");
				Console.ReadKey();
				Console.WriteLine();
				log.Close();
				return;
			}
			log.Info("Loaded configuration file.");

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

			//Create console command thread
			ConsoleThread = new Thread(() => API.Threads.ConsoleCommand.main(log)) {
				IsBackground = true
			};
			ConsoleThread.Start();

			//Create worker threads
			log.Info("Creating worker threads.");
			int threadCount = (int)Settings["performanceSettings"]["workerThreadCount"];
			for (int i = 0; i < threadCount; i++) {
				var worker = new RequestWorker(CreateConnection(), requestQueue, "RequestWorker" + (i + 1), log);
				worker.Start();
				RequestWorkers.Add(worker);
			}

			// Create listener thingy.
			HttpListener listener = new HttpListener();
			foreach (string address in addresses)
				listener.Prefixes.Add("http://" + address + "/");
			ListenerThread = new Listener(listener, requestQueue, "ListenerThread", log);
			ListenerThread.Start();
			log.Config("Finished setup");

			// Wait until all threads are terminated
			foreach (var worker in RequestWorkers) {
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
		public static void IOSetup()
		{
			// Create folders
			Directory.CreateDirectory("Logs");

			// Compress previous log
			if (File.Exists("Logs/latest.log"))
			{
				var lastArchive = Directory.GetFiles("Logs").OrderBy(x => File.GetCreationTime(x)).LastOrDefault();
				if (lastArchive != null)
				{
					using (var archive = ZipFile.Open(lastArchive, ZipArchiveMode.Update))
						archive.CreateEntryFromFile("Logs/latest.log", "latest.log");
					File.Delete("Logs/latest.log");
				}
			}
		}
	}
}
