﻿using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	abstract partial class RequestHandler {
		/// <summary>
		/// Handles requests with requestType "registerUser".
		/// </summary>
		/// <param name="request">The JObject containing the request received from the client.</param>
		/// <returns>A JObject containing the request response, which can then be sent to the client.</returns>
		[IgnoreUserToken]
		public JObject registerUser(JObject request) {
			//Verify user details
			request.TryGetValue("username", out JToken usernameValue);
			request.TryGetValue("password", out JToken passwordValue);
			if (usernameValue == null || passwordValue == null || usernameValue.Type == JTokenType.Null || passwordValue.Type == JTokenType.Null) {
				return Templates.MissingArguments("username, password");
			}
			string username = usernameValue.ToString();
			string password = passwordValue.ToString();

			//Verify username
			if (!Misc.verifyUsernameLength(username) || !Misc.verifyUsernameRegex(username)) {
				return Templates.InvalidUsername;
			}


			//Check if password is a SHA-512 hash.
			//This checks whether the password string is the correct length for a SHA-512 hash, and if it is a proper hexadecimal number.
			//It's possible for people directly calling the API to create a user with a password that wasn't salted with their username (should we fix this?), but I doubt anyone would do that.
			//Also regex is weird and I do not like it.
			if (password.Length != 128 && !System.Text.RegularExpressions.Regex.IsMatch(password, @"\A\b[0-9a-fA-F]+\b\Z")) {
				return Templates.InvalidPassword;
			}

			//Check if username already exists
			if (GetObject<User>(username, "Username") != null) {
				return Templates.AlreadyExists(username);
			}

			//Create user
			long token = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			User user = new User(username, password, token, UserPermission.User);
			user.Upload(Connection);

			//Create response object
			JObject response = new JObject() {
				{"reason", null},
				{"responseData", new JObject() {
					{"userToken", (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds },
					{"permissionLevel", 0}
				}}
			};

			return response;
		}
	}
}