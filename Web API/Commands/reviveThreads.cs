using API.Threads;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Restarts all worker threads that are no longer running.")]
		public static void ReviveThreads(string[] _)
		{
			int successes = 0;
			int restarts = 0;
			for (int i = 0; i < Program.RequestWorkers.Length; i++)
			{
				var worker = Program.RequestWorkers[i];
				if (worker == null || !worker.IsAlive || worker.State == ConnectionState.Closed || worker.State == ConnectionState.Broken)
				{
					restarts++;
					worker?.Dispose();
					try
					{
						Program.RequestWorkers[i] = new RequestWorker(
							Program.CreateConnection(),
							Program.ListenerThread.Queue,
							worker?.Name ?? "RequestWorker" + (i + 1),
							log
						);
						Program.RequestWorkers[i].Start();
						successes++;
					}
					catch (MySqlException e)
					{
						log.Error($"Error while creating connection for {"RequestWorker" + (i + 1)}: {e.GetType().Name}: {e.Message}", false);
					}
				}
			}
			log.Info($"Restarted {successes}/{restarts} workers");
		}
	}
}
