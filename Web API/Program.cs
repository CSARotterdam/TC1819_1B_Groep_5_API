﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MySQLWrapper;

namespace API {
	class Program {
		public static TechlabMySQL wrapper;
		public static bool ManualError = false;
		private static int _errorCode;
		public static int ErrorCode{
			get { return _errorCode; }
			set {
				_errorCode = value;
				Console.WriteLine("Error code changed to " + _errorCode.ToString());
				if(_errorCode == 0){
					Console.WriteLine("Error state disabled. Now accepting requests.");
				} else {
					Console.WriteLine("Error state enabled. All requests will be refused.");
				}
			}
		}

		public static void Main() {
			//Load configuration file
			dynamic Settings = Config.loadConfig();

			//Check if config contains all necessary info to start. If it doesn't, abort launch.
			bool validConfig = true;
			if(Settings.databaseSettings.username == null || Settings.databaseSettings.password == null || Settings.databaseSettings.serverAddress == null || Settings.databaseSettings.database == null) {
				Console.WriteLine("Error: Incomplete database configuration.");
				validConfig = false;
			}
			if((bool)Settings.connectionSettings.autodetect && Settings.connectionSettings.serverAddresses.Count == 0) {
				Console.WriteLine("Error: Missing server address.");
				validConfig = false;
			}
			if (!validConfig) {
				Console.WriteLine("The server failed to start because there is at least one invalid setting. Please check the server configuration!");
				Console.WriteLine("Press the any key to exit.");
				Console.ReadLine();
				return;
			}

			//Get local IP address, if autodetect is enabled in settings.
			List<string> addresses = Settings.connectionSettings.serverAddresses.ToObject<List<string>>();
			if ((bool)Settings.connectionSettings.autodetect) {
				using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
					socket.Connect("8.8.8.8", 65530);
					IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
					string address = endPoint.Address.ToString();
					addresses.Add(address);
				}
			}

			//Create request queue
			BlockingCollection<HttpListenerContext> requestQueue = new BlockingCollection<HttpListenerContext>();

			//Create console command thread
			Thread consoleThread = new Thread(() => API.Threads.ConsoleCommand.main());
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
			Console.WriteLine("Creating worker threads.");
			int threadCount = (int)Settings.performanceSettings.workerThreadCount;
			Thread[] threadList = new Thread[threadCount];
			for (int i = 0; i != threadCount; i++) {
				Thread workerThread = new Thread(() => API.Threads.RequestWorker.main(requestQueue)) {
					Name = "WorkerThread" + i.ToString()
				};
				workerThread.Start();
				threadList[i] = workerThread;
			}
			Console.WriteLine("Finished creating worker threads.");

			// Create listener thingy.
			HttpListener listener = new HttpListener();
			foreach (string address in addresses) {
				listener.Prefixes.Add("http://" + address + "/");
			}
			listener.Start();
			System.Console.WriteLine("Now listening for requests.");

			// Main loop
			while (true) {
				// Wait for request 
				requestQueue.Add(listener.GetContext());
				Console.WriteLine("Received and enqueued a request!");
			}
		}
	}
}
