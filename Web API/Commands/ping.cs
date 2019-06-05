using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Pings the database and prints it's results.")]
		public static void Ping(string[] args)
		{
			List<double> delays = new List<double>();
			int pingCount = 4;
			try { pingCount = int.Parse(args[0]); } catch (Exception) { }
			for (int i = 0; i < pingCount; i++)
			{
				if (i != 0) Thread.Sleep(1000 - (int)delays.Last());
				timer.Start();
				bool status = Connection.Ping();
				timer.Stop();
				if (status) log.Info($"Reply after {Misc.FormatDelay(timer, 1)}");
				else
				{
					log.Info($"Failed after {Misc.FormatDelay(timer, 1)}");
					timer.Reset();
					break;
				}
				delays.Add(timer.Elapsed.TotalMilliseconds);
				timer.Reset();
			}
			if (delays.Count > 1) log.Info($"Average: {Math.Round(delays.Average(), 2)} ms");
		}
	}
}
