using API.Threads;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Displays info about the state of the program.")]
		public static void Status(string[] _)
		{
			var types = new List<string>();
			var names = new List<string>();
			var threadIds = new List<string>();
			var states = new List<string>();
			var connectionStates = new List<string>();
			var pings = new List<string>();

			// Add headers
			types.Add("TYPE");
			names.Add("NAME");
			threadIds.Add("ID");
			states.Add("STATE");
			connectionStates.Add("CSTATE");
			pings.Add("PING");

			void AddNonDB(Type type, string name, int? id, System.Threading.ThreadState? state)
			{
				types.Add(type.Name);
				names.Add(name ?? "N/A");
				threadIds.Add(id?.ToString() ?? "-");
				states.Add(state?.ToString() ?? System.Threading.ThreadState.Stopped.ToString());
				connectionStates.Add("");
				pings.Add("");
			}

			void Add(Type type, string name, int? id, System.Threading.ThreadState? state, ConnectionState? cstate, Stopwatch ping)
			{
				ping.Stop();
				types.Add(type.Name);
				names.Add(name ?? "N/A");
				threadIds.Add(id?.ToString() ?? "-");
				states.Add(state?.ToString() ?? System.Threading.ThreadState.Stopped.ToString());
				connectionStates.Add(cstate?.ToString() ?? ConnectionState.Closed.ToString());
				pings.Add(cstate != null ? Misc.FormatDelay(ping, 1) : "0 ns");
			}

			var timer = new Stopwatch();

			timer.Start();
			Connection.Ping();
			Add(Program.ConsoleThread.GetType(), Program.ConsoleThread.Name, Program.ConsoleThread.ManagedThreadId, Program.ConsoleThread.ThreadState, Connection.State, timer);
			foreach (var worker in Program.RequestWorkers)
			{
				timer.Restart();
				worker?.Ping();
				Add(typeof(RequestWorker), worker?.Name, worker?.ManagedThreadId, worker?.ThreadState, worker?.State, timer);
			}
			AddNonDB(Program.ListenerThread.GetType(), Program.ListenerThread.Name, Program.ListenerThread.ManagedThreadId, Program.ListenerThread.ThreadState);

			string Fit(string text, int length, string spacer = " | ", char filler = ' ') => text + new string(filler, length - text.Length) + spacer;

			int typeLen = types.Max(x => x.Length);
			int nameLen = names.Max(x => x.Length);
			int idlen = threadIds.Max(x => x.Length);
			int stateLen = states.Max(x => x.Length);
			int cstateLen = connectionStates.Max(x => x.Length);
			int pingLen = pings.Max(x => x.Length);
			
			lock (log)
			{
				var builder = new StringBuilder();
				// Write headers
				builder.Append(Fit("", 0, "| "));
				builder.Append(Fit(threadIds[0], idlen));
				builder.Append(Fit(names[0], nameLen));
				builder.Append(Fit(types[0], typeLen));
				builder.Append(Fit(states[0], stateLen));
				builder.Append(Fit(connectionStates[0], cstateLen));
				builder.Append(Fit(pings[0], pingLen));
				log.Info(builder.ToString());
				builder.Clear();

				builder.Append(Fit("", 0, "|-", '-'));
				builder.Append(Fit("", idlen, "-|-", '-'));
				builder.Append(Fit("", nameLen, "-|-", '-'));
				builder.Append(Fit("", typeLen, "-|-", '-'));
				builder.Append(Fit("", stateLen, "-|-", '-'));
				builder.Append(Fit("", cstateLen, "-|-", '-'));
				builder.Append(Fit("", pingLen, "-|", '-'));
				log.Info(builder.ToString());
				builder.Clear();
				
				// Yes, please calm down with the applause. I am very aware that this is the best method name in existence.
				// Oh please, all this praise is too much for one so humble such as myself.
				int parseOrDont(string s)
				{
					int.TryParse(s, out int res);
					return res;
				}

				foreach (var id in threadIds.TakeLast(threadIds.Count - 1).OrderBy(x => parseOrDont(x)))
				{
					int i = threadIds.IndexOf(id);
					builder.Append(Fit("", 0, "| "));
					builder.Append(Fit(id, idlen));
					builder.Append(Fit(names[i], nameLen));
					builder.Append(Fit(types[i], typeLen));
					builder.Append(Fit(states[i], stateLen));
					builder.Append(Fit(connectionStates[i], cstateLen));
					builder.Append(Fit(pings[i], pingLen));
					log.Info(builder.ToString());
					builder.Clear();
				}
			}
		}
	}
}
