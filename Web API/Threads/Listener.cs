using Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace API.Threads {
	class Listener {
		/// <summary>
		/// Gets whether or not this Listener's worker thread is alive.
		/// </summary>
		public bool IsAlive => workerThread.IsAlive;
		public ThreadState ThreadState => workerThread.ThreadState;
		public int ManagedThreadId => workerThread.ManagedThreadId;
		public string Name { get { return workerThread?.Name; } set { workerThread.Name = value; } }

		public BlockingCollection<HttpListenerContext> Queue { get; }
		public Logger Log { get; set; }

		private readonly HttpListener listener;
		private readonly Thread workerThread;

		public Listener(HttpListener listener, BlockingCollection<HttpListenerContext> queue, string name = null, Logger logger = null)
		{
			this.listener = listener;
			workerThread = new Thread(ThreadStart) { Name = name ?? GetType().Name, IsBackground = true };
			Queue = queue;
			Log = logger;
		}

		/// <summary>
		/// Waits for an incoming request from the <see cref="HttpListener"/> and puts it's
		/// <see cref="HttpListenerContext"/> in <see cref="Queue"/>.
		/// <para>Continuously looped by the worker thread.</para>
		/// </summary>
		public void Run()
		{
			// Wait for request
			Queue.Add(listener.GetContext());
			Log.Fine("Received and enqueued a request.");
		}

		#region Thread control
		/// <summary>
		/// Wraps <see cref="Run"/> in a fatal exception handler.
		/// </summary>
		private void ThreadStart()
		{
			try
			{
				// Main loop
				while (listener.IsListening) Run();
			}
			catch (Exception e)
			{
				if (!(e is ThreadInterruptedException))
					Log.Fatal($"Exception in thread '{Thread.CurrentThread.Name}': {e.GetType().Name}: {e.Message}", e);
			}
		}

		/// <summary>
		/// Starts this listener and it's worker thread.
		/// </summary>
		public void Start()
		{
			Log.Config($"Starting thread '{this}'");
			listener.Start();
			workerThread.Start();
		}

		/// <summary>
		/// Stops this listener's associated <see cref="HttpListener"/>. This listener's worker thread can only
		/// terminate along with the mainthread.
		/// </summary>
		public void Stop()
		{
			listener.Close();
			workerThread.Interrupt();
		}
		#endregion

		/// <summary>
		/// Returns a string representing this <see cref="Listener"/>.
		/// </summary>
		public override string ToString() => workerThread?.Name ?? base.ToString();
	}
}
