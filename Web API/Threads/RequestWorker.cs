using API.Requests;
using Logging;
using MySQLWrapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace API.Threads
{
	sealed class RequestWorker : RequestHandler {
		/// <summary>
		/// Gets whether or not this requestWorker's thread is alive.
		/// </summary>
		public bool IsAlive => workerThread.IsAlive;
		public System.Threading.ThreadState ThreadState => workerThread.ThreadState;
		public int ManagedThreadId => workerThread.ManagedThreadId;
		public string Name { get { return workerThread?.Name; } set { workerThread.Name = value; } }

		private readonly BlockingCollection<HttpListenerContext> RequestQueue;
		private readonly Stopwatch timer = new Stopwatch();
		private readonly Thread workerThread;

		/// <summary>
		/// Creates a new instance of <see cref="RequestWorker"/>.
		/// </summary>
		/// <param name="connection">The connection to use for all handlers.</param>
		/// <param name="requestQueue">The queue to take incoming requests from.</param>
		/// <param name="logger">An optional logger for all handlers.</param>
		public RequestWorker(TechlabMySQL connection, BlockingCollection<HttpListenerContext> requestQueue, string name = null, Logger logger = null)
			: base(connection, logger)
		{
			RequestQueue = requestQueue;
			workerThread = new Thread(ThreadStart) { Name = name ?? GetType().Name };
		}

		/// <summary>
		/// Waits for a request from <see cref="RequestQueue"/>, processes it and returns an appropriate response.
		/// </summary>
		public void Run()
		{
			// Take request from queue
			HttpListenerContext context = RequestQueue.Take();
			HttpListenerRequest request = context.Request;

			timer.Restart();

			// Send a HTTP 415 UnsupportedMediaType if the content type isn't a json
			if (request.ContentType != "application/json")
			{
				int size = SendHTMLError(context, "If at first you don't succeed, fail 5 more times.", HttpStatusCode.UnsupportedMediaType);
				timer.Stop();
				Log.Fine(GetTimedMessage(timer, $"Recieved non-JSON request. Sent 'UnsupportedMediaType' error with {size} bytes."));
				return;
			}

			// Send a 400 BadRequest if the request has no body
			if (!request.HasEntityBody)
			{
				int size = SendMessage(context, "Empty body data", HttpStatusCode.BadRequest);
				timer.Stop();
				Log.Fine(GetTimedMessage(timer, $"Recieved request with no body. Sent 'BadRequest' error with {size} bytes."));
				return;
			}

			// Send an error if the database isn't available
			if (!Connection.AutoReconnect && !Connection.Ping())
			{
				Connection.Reconnect();
				if (!Connection.Ping())
				{
					int size = SendMessage(context, Templates.ServerError().ToString(), HttpStatusCode.InternalServerError);
					timer.Stop();
					Log.Warning(GetTimedMessage(timer, $"Database connection failed for '{workerThread.Name}'. " +
						$"Sent 'ServerError' with status 'InternalServerError' with {size} bytes."));
					return;
				}
			}

			var requestContent = JObject.Parse(new StreamReader(
				request.InputStream,
				request.ContentEncoding).ReadToEnd()
			);

			JObject response;
			HttpStatusCode statusCode = HttpStatusCode.OK;
			try
			{
				// Parse request
				response = ParseRequest(requestContent);
				timer.Stop();
				Log.Trace(GetTimedMessage(timer, $"Processed request" +
					$"{(requestContent.ContainsKey("requestType") ? $" '{requestContent["requestType"]}'" : "")} " +
					$"with {request.ContentLength64} bytes."));
				timer.Restart();
			}
			catch (Exception e)
			{
				// Get inner cause if there is one
				if (e.InnerException != null) e = e.InnerException;

				// Catch unhandeled exceptions during parsing
				timer.Stop();
				Log.Error(GetTimedMessage(timer, $"Error during request" +
					$"{(requestContent.ContainsKey("requestType") ? $" '{requestContent["requestType"]}'" : "")}: " +
					$"{e.GetType().Name}: {e.Message}"), e, true);

				// Send 'ServerError' response with InternalServerError status
				response = Templates.ServerError();
				statusCode = HttpStatusCode.InternalServerError;
				timer.Restart();
			}

			// Send response
			context.Response.ContentType = "application/json";
			try {
				int responseSize = SendMessage(context, response.ToString(), statusCode);
				timer.Stop();
				Log.Trace(GetTimedMessage(timer, $"Sent response" +
					$"{(response.ContainsKey("reason") && response["reason"].ToString().Any() ? $" '{response["reason"]}'" : "")} " +
					$"with {responseSize} bytes."));
			} catch(HttpListenerException e) {
				Log.Error("Error sending response: "+e.Message, e, false);
			}
			
			timer.Reset();
		}

		#region Thread control
		/// <summary>
		/// Starts this RequestWorker's thread.
		/// </summary>
		public void Start()
		{
			Log.Config($"Starting thread '{this}'");
			workerThread.Start();
		}

		/// <summary>
		/// Stops this worker by interrupting it's thread.
		/// </summary>
		public void Stop() => workerThread.Interrupt();

		/// <summary>
		/// Blocks until this worker's thread has stopped.
		/// </summary>
		public void Join() => workerThread.Join();

		/// <summary>
		/// Wraps <see cref="Run"/> in a fatal exception handler.
		/// </summary>
		private void ThreadStart()
		{
			try
			{
				while (true) Run();
			}
			catch (Exception e)
			{
				if (!(e is ThreadInterruptedException))
					Log.Fatal($"Exception in thread '{Thread.CurrentThread.Name}': {e.GetType().Name}: {e.Message}", e);
			}
		}
		#endregion

		/// <summary>
		/// Sends an HTML page with an error code and message to the given <see cref="HttpListenerContext"/>.
		/// </summary>
		/// <param name="context">The HttpListenerContext to respond to.</param>
		/// <param name="message">The message to send back.</param>
		/// <param name="statusCode">The response status code.</param>
		private static int SendHTMLError(HttpListenerContext context, string message, HttpStatusCode statusCode) {
			// Send error page
			string responseString = "<HTML><BODY><H1>" + (int)statusCode + " " + statusCode + "</H1>" + message + "</BODY></HTML>";
			return SendMessage(context, responseString, statusCode);
		}

		/// <summary>
		/// Sends a response to the given <see cref="HttpListenerContext"/>.
		/// </summary>
		/// <param name="context">The HttpListenerContext to respond to.</param>
		/// <param name="message">The message to send back.</param>
		/// <param name="statusCode">The respones status code. Defaults to <see cref="HttpStatusCode.OK"/>.</param>
		/// <returns></returns>
		private static int SendMessage(HttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK) {
			// Get response
			HttpListenerResponse response = context.Response;
			response.StatusCode = (int)statusCode;
			// Write message
			byte[] buffer = Encoding.UTF8.GetBytes(message);
			Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
			// Return amount of bytes in response
			return buffer.Length;
		}

		/// <summary>
		/// Returns the message with a formatted prefix indicating the elapsed time of the stopwatch.
		/// </summary>
		/// <param name="stopwatch">The stopwatch to take the time from.</param>
		/// <param name="decimals">The amount of decimals to include in the prefix.</param>
		/// <param name="message">The message to apply the prefix to.</param>
		/// <param name="format">(Optional) The format of the prefix.</param>
		private static string GetTimedMessage(Stopwatch stopwatch, string message, int decimals = 2, string format = "({0}) ")
			=> string.Format(format, Misc.FormatDelay(stopwatch, decimals)) + message;

		/// <summary>
		/// Returns a string representing this <see cref="RequestWorker"/>.
		/// </summary>
		public override string ToString() => workerThread?.Name ?? base.ToString();
	}
}
