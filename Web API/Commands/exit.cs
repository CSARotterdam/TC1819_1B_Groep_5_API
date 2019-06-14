using System.Diagnostics;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Alias for 'EXIT'.")]
		public static void Quit(string[] args) => Exit(args);

		[MonitoringDescription("Shuts down all worker threads and terminates the program.")]
		public static void Exit(string[] _)
		{
			foreach (var worker in Program.RequestWorkers)
				worker?.Stop();
			Program.ListenerThread.Stop();
		}
	}
}
