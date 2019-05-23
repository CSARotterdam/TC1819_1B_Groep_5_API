using MySQLWrapper.Data;
using System;
using System.Collections.Generic;
using System.Text;
using static API.Requests.Requests;

namespace API.Commands {
	static partial class CommandMethods {
		public static void logout(string[] tokens) {
			//Check if enough tokens were given
			if(tokens.Length < 2) {
				Console.WriteLine("Usage: logout <username>");
				return;
			}

			//Get the user object. Show an error if doesn't exist.
			User user = getObject<User>(tokens[1]);
			if (user == null) {
				Console.WriteLine("No such user.");
				return;
			}

			//Set user token to 0 to log them out.
			user.Token = 0;
			user.Update(wrapper);
			Console.WriteLine("User now logged out.");
		}
	}
}
