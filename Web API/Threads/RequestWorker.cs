using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Logging;
using API.Requests;
using System.Reflection;

namespace API.Threads {
	class RequestWorker {

		//RequestWorker threads takes and processes incoming requests from the requestQueue (which are added to the queue by the Listener thread.
		public static void main(Logger log, BlockingCollection<HttpListenerContext> requestQueue) {
			MethodInfo[] methods = typeof(RequestMethods).GetMethods();

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
				MethodInfo requestMethod = null;
				foreach (MethodInfo method in methods) {
					if (method.Name == requestContent["requestType"].ToString()){
						requestMethod = method;
					}
				}

				JObject responseJson;
				if (requestMethod != null) {
					Object[] methodParams = new object[1] { requestContent };
					responseJson = (JObject)requestMethod.Invoke(null, methodParams);
				} else {
					log.Error("Request has invalid requestType value: " + requestContent["requestType"].ToString());
					statusCode = HttpStatusCode.BadRequest;
					responseJson = new JObject(){
						{"requestID", requestContent["requestID"].ToString()},
						{"requestData", new JObject(){
							{"Error", "Invalid requestType value"}
						}}
					};
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
