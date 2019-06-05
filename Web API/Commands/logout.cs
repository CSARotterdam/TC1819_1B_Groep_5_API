using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using System;
using System.Diagnostics;
using System.Linq;

namespace API.Commands {
	static partial class CommandMethods
	{
		[MonitoringDescription("Forces the given user to log out.")]
		public static void Logout(string[] tokens) {
			//Check if enough tokens were given
			if (tokens.Length < 1) {
				Console.WriteLine("Usage: logout <username>");
				return;
			}

			//Get the user object. Show an error if doesn't exist.
			var condition = new MySqlConditionBuilder()
				.Column("username")
				.Equals(tokens[0], MySqlDbType.String);
			User user = Program.Connection?.Select<User>(condition, (0, 1)).FirstOrDefault();
			if (user == null) {
				Console.WriteLine("No such user.");
				return;
			}

			//Set user token to 0 to log them out.
			user.Token = 0;
			user.Update(Connection);
			Console.WriteLine("User now logged out.");
		}
	}
}
