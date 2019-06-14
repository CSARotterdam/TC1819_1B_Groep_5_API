using System.Diagnostics;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Reloads the config.json file.")]
		public static void ReloadConfig(string[] _)
		{
			lock (Program.Settings)
				Program.Settings = Config.loadConfig();
			log.Info("Successfully reloaded config");
		}
	}
}
