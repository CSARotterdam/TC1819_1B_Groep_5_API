﻿using Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace API {
	class Listener {

		//For someone called the 'Listener,' you do an awful lot of shouting...
		public static void main(Logger log, HttpListener listener, BlockingCollection<HttpListenerContext> requestQueue) {
			log.Info("Thread Listener now running.");
			listener.Start();

			// Main loop
			while (true) {
				// Wait for request 
				requestQueue.Add(listener.GetContext());
				log.Fine("Received and enqueued a request.");
			}
		}
	}
}