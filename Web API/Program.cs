using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;
using MySQLWrapper;
using System.IO;
using System.Configuration;
using System.Collections.Concurrent;

namespace Web_API {
	class Program {
		public static void Main() {
			// Get local IP address
			String address;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				address = endPoint.Address.ToString();
			}
			Console.WriteLine("Local IP address is "+ address);

			//Create request queue
			BlockingCollection<HttpListenerContext> requestQueue = new BlockingCollection<HttpListenerContext>();

			//Create console command thread
			Thread consoleThread = new Thread(() => ConsoleCommand.main());
			consoleThread.Start();

			//Create database connection
			Thread databaseMaintainerThread = new Thread(() => DatabaseMaintainer.main());
			databaseMaintainerThread.Start();

			//Create worker threads
			Console.WriteLine("Creating worker threads.");
			int threadCount = 5;
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
			listener.Prefixes.Add("http://localhost/");
			listener.Prefixes.Add("http://" + address + "/");
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
