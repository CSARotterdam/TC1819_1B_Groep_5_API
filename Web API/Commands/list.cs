using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Alias for 'LIST'.")]
		public static void Help(string[] args) => List(args);

		[MonitoringDescription("Lists all available commands.")]
		public static void List(string[] _)
		{
			var methods = typeof(CommandMethods).GetMethods().Where(x => x.IsStatic).OrderBy(x => x.Name);
			int maxNameLen = methods.Max(x => x.Name.Length);

			foreach (var method in methods)
			{
				var description = (MonitoringDescriptionAttribute)method.GetCustomAttribute(typeof(MonitoringDescriptionAttribute));
				Console.WriteLine(method.Name.ToUpper() + (description == null ? "" : new string(' ', maxNameLen - method.Name.Length) + " \t" + description.Description));
			}
		}
	}
}
