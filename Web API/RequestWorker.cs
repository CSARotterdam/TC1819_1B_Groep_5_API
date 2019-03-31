using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Web_API {
	class RequestWorker {
		public static void main(BlockingCollection<HttpListenerContext> requestQueue) {
			Console.WriteLine("Thread " + Thread.CurrentThread.Name + " now running.");
			while (true) {
				// Get request
				HttpListenerContext context = requestQueue.Take();
				HttpListenerRequest request = context.Request;

				//Check if content type is application/json. Send a 415 UnsupportedMediaType if it isn't.
				if (request.ContentType != "application/json") {
					Console.WriteLine("Request has invalid content type. Sending error response and ignoring!");
					sendHTMLError(context, "If at first you don't succeed, fail 5 more times.", HttpStatusCode.UnsupportedMediaType);
					continue;
				}
				//Check if request has body data. Send a 400 BadRequest if it doesn't.
				if (!request.HasEntityBody) {
					Console.WriteLine("Request has no body data. Sending error response and ignoring!");
					sendMessage(context, "Empty body data", HttpStatusCode.BadRequest);
				}

				System.IO.Stream body = request.InputStream;
				System.Text.Encoding encoding = request.ContentEncoding;
				System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
				dynamic requestContent = JObject.Parse(reader.ReadToEnd());
				Console.WriteLine(requestContent.hello);

				// Create response
				HttpListenerResponse response = context.Response;
				response.ContentType = "application/json";
				JObject json = new JObject {
					{ "goodbye", Thread.CurrentThread.Name }
				};
				sendMessage(context, json.ToString(), HttpStatusCode.OK);
			}
		}

		static void sendHTMLError(HttpListenerContext context, string message, HttpStatusCode statusCode) {
			//Send error page
			string responseString = "<HTML><BODY><H1>" + (int)statusCode + " " + statusCode + "</H1>" + message + "</BODY></HTML>";
			sendMessage(context, responseString, statusCode);
		}

		static void sendMessage(HttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK) {
			HttpListenerResponse response = context.Response;
			response.StatusCode = (int)statusCode;
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
		}
	}
}
