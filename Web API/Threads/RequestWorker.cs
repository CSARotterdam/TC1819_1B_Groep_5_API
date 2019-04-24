using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using API.Requests;
using Logging;

namespace API.Threads {
	class RequestWorker {
		public static void main(Logger log, BlockingCollection<HttpListenerContext> requestQueue) {
			log.Info("Thread " + Thread.CurrentThread.Name + " now running.");
			while (true) {
				// Wait for request.
				HttpListenerContext context = requestQueue.Take();
				HttpListenerRequest request = context.Request;
				log.Fine("Processing request.");

				// If API error state is true, cancel the request (because it'll fail anyway) and send an error.
				if (Program.ErrorCode != 0){
					sendMessage(context, "Code"+Program.ErrorCode.ToString(), HttpStatusCode.InternalServerError);
					continue;
				}
				// Check if content type is application/json. Send a HTTP 415 UnsupportedMediaType if it isn't.
				if (request.ContentType != "application/json") {
					log.Error("Request has invalid content type. Sending error response and ignoring!");
					sendHTMLError(context, "If at first you don't succeed, fail 5 more times.", HttpStatusCode.UnsupportedMediaType);
					continue;
				}
				// Check if request has body data. Send a 400 BadRequest if it doesn't.
				if (!request.HasEntityBody) {
					log.Error("Request has no body data. Sending error response and ignoring!");
					sendMessage(context, "Empty body data", HttpStatusCode.BadRequest);
				}

				//Convert request data to JObject
				System.IO.Stream body = request.InputStream;
				System.Text.Encoding encoding = request.ContentEncoding;
				System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
				JObject requestContent = JObject.Parse(reader.ReadToEnd());

				// Handle request
				HttpStatusCode statusCode = HttpStatusCode.OK;
				JObject responseJson = new JObject();

				try {
					switch (requestContent["requestType"].ToString()) {
						case "login":
							responseJson = LoginRequest.Login(requestContent);
							break;

						default:
							throw new InvalidRequestTypeException(requestContent["requestType"].ToString());

					}
				} catch(InvalidRequestTypeException e){
					log.Error("Invalid request type "+e);
				}
				
				// Create & send response
				HttpListenerResponse response = context.Response;
				response.ContentType = "application/json";
				sendMessage(context, responseJson.ToString(), statusCode);
				log.Fine("Request processed successfully.");
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
