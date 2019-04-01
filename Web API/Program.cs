using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Web_API {
	class Program {
		public static void Main() {
			//Load configuration file
			dynamic Settings = Config.loadConfig();

			//Check if config contains all necessary info to start. If it doesn't, abort launch.
			bool validConfig = true;
			if(Settings.databaseSettings.username == null || Settings.databaseSettings.password == null || Settings.databaseSettings.serverAddress == null) {
				Console.WriteLine("Error: Incomplete database configuration.");
				validConfig = false;
			}
			if((bool)Settings.connectionSettings.autodetect && Settings.connectionSettings.serverAddresses.Count == 0) {
				Console.WriteLine("Error: Missing server address.");
				validConfig = false;
			}
		
			if (!validConfig) {
				Console.WriteLine("The server failed to start because some required data is missing. Please check the configuration!");
				Console.ReadLine();
			} else {
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
				Thread consoleThread = new Thread(() => ConsoleCommand.main());
				consoleThread.Start();

				//Create database maintainer thread
				Thread databaseMaintainerThread = new Thread(() => DatabaseMaintainer.main());
				databaseMaintainerThread.Start();

				//Create worker threads
				Console.WriteLine("Creating worker threads.");
				int threadCount = (int)Settings.performanceSettings.workerThreadCount;
				Thread[] threadList = new Thread[threadCount];
				for (int i = 0; i != threadCount; i++) {
					Thread workerThread = new Thread(() => RequestWorker.main(requestQueue));
					workerThread.Name = "WorkerThread" + i.ToString();
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
}
