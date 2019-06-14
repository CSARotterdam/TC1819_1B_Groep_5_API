using Logging;
using MySql.Data.MySqlClient;
using MySQLWrapper;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using static API.Requests.RequestMethodAttributes;

namespace API.Requests {
	/// <summary>
	/// The respone building component of a request listener. 
	/// </summary>
	abstract partial class RequestHandler {
		/// <summary>
		/// A DateTime instance representing 'Epoch'.
		/// </summary>
		private static DateTime Epoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// The <see cref="Logger"/> instance assigned to this <see cref="RequestHandler"/>.
		/// </summary>
		public Logger Log { get; set; }

		/// <summary>
		/// The state of the underlying <see cref="MySqlConnection"/>.
		/// </summary>
		public ConnectionState State => Connection?.State ?? ConnectionState.Closed;

		/// <summary>
		/// The <see cref="TechlabMySQL"/> connection assigned to this <see cref="RequestHandler"/>.
		/// </summary>
		protected TechlabMySQL Connection { get; }
		/// <summary>
		/// The <see cref="User"/> from whom the requests are coming from. This value must be
		/// set before any request.
		/// </summary>
		protected User CurrentUser { get; set; }

		/// <summary>
		/// The maximum duration of a loan, as specified in the <see cref="Program.Settings"/>.
		/// </summary>
		private TimeSpan MaxLoanDuration => (TimeSpan)Program.Settings["requestSettings"]["maxLoanDuration"];

		/// <summary>
		/// Creates a new instance of <see cref="RequestHandler"/>.
		/// </summary>
		/// <param name="connection">The connection to use for all handlers.</param>
		/// <param name="log">An optional logger for all handlers.</param>
		protected RequestHandler(TechlabMySQL connection, Logger log = null)
		{
			if (connection.State != ConnectionState.Open)
			{
				try
				{ connection.Open(); }
				catch (Exception e)
				{ throw new OperationCanceledException("Could not open connection.", e); }
			}
			Connection = connection;
			Log = log ?? new Logger(Level.OFF);
		}

		/// <summary>
		/// Passes the request JSON an appropriate handler.
		/// </summary>
		/// <param name="request">The request JSON to pass to the handler.</param>
		/// <returns>A response JSON built by the handler.</returns>
		protected JObject ParseRequest(JObject request)
		{
			request.TryGetValue("requestType", out JToken requestName);
			request.TryGetValue("requestData", out JToken requestData);

			if (requestName == null)
			{
				Log.Fine("Skipped malformed request with no requestType.");
				return Templates.MalformedRequest("requestType expected but not found");
			}
			if (requestData == null)
			{
				Log.Fine("Skipped malformed request with no requestData.");
				return Templates.MalformedRequest("requestData expected but not found");
			}

			// Get method to handle the incoming request
			var handler = typeof(RequestHandler).GetMethod(requestName.ToString());
			if (handler == null)
			{
				Log.Fine($"Skipped request with unknown type '{requestName}'");
				return Templates.InvalidRequestType(requestName.ToString());
			}

			// Begin preparations for handler invocation
			JObject response;
			CurrentUser = null;
			Log.Info($"Processing request '{requestName}'");

			// Get handler attributes
			var attributes = handler.GetCustomAttributes<RequestAttribute>();
			var ignoreTokenAttrib = (IgnoreUserToken)attributes.FirstOrDefault(x => x is IgnoreUserToken);
			var permissionAttrib = (RequiresPermissionLevel)attributes.FirstOrDefault(x => x is RequiresPermissionLevel);

			// Return alternative response if verifications fail
			if (ignoreTokenAttrib == null)
				if (!VerifyToken(request, out response))
					return response;

			if (permissionAttrib != null)
				if (!VerifyPermission(request, permissionAttrib.Permission, out response))
					return response;

			// Invoke handler and return response
			return (JObject)handler.Invoke(this, new object[] { requestData });
		}

		#region Verification Methods
		/// <summary>
		/// Returns false if the given token from in <paramref name="request"/> as a token that is older
		/// than the expiration time specified in <see cref="Program.Settings"/> or doesn't match the token
		/// in <see cref="CurrentUser"/>.
		/// </summary>
		/// <remarks>
		/// In case this returns false, the response value must be returned to the client.
		/// </remarks>
		/// <param name="request">The request from whom to take the user info.</param>
		/// <param name="response">A response JObject. In case of an error, this will be an error JSON. Otherwise always null.</param>
		/// <returns>True if the verification went succesfully. Otherwise false.</returns>
		private bool VerifyToken(JObject request, out JObject response)
		{
			if (!GetUser(request, out response))
				return false;

			// Calculate token age
			long expiration = (long)Program.Settings["authenticationSettings"]["expiration"];
			long token = (long)request["token"];
			long tokenAge = (long)(DateTime.UtcNow - Epoch.AddSeconds(token).ToLocalTime()).TotalSeconds;

			// In case of token mismatch or expired token, return "ExpiredToken"
			if (token != CurrentUser.Token || tokenAge > expiration)
			{
				response = Templates.ExpiredToken;
				return false;
			}

			// Verification succeeded
			return true;

		}

		/// <summary>
		/// Returns false if the <see cref="CurrentUser"/> from <see cref="GetUser(JObject, out JObject)"/>
		/// does not have the required permission level.
		/// </summary>
		/// <remarks>
		/// In case this returns false, the response value must be returned to the client.
		/// </remarks>
		/// <param name="request">The request from whom to take the user info.</param>
		/// <param name="permission">The minimum permission level <see cref="CurrentUser"/> must have.</param>
		/// <param name="response">A response JObject. In case of an error, this will be an error JSON. Otherwise always null.</param>
		/// <returns>True if the verification went succesfully. Otherwise false.</returns>
		private bool VerifyPermission(JObject request, UserPermission permission, out JObject response)
		{
			if (!GetUser(request, out response))
				return false;

			// In case the user's permission is less than required, return "AccessDenied"
			if (CurrentUser.Permission < permission)
			{
				response = Templates.AccessDenied;
				return false;
			}

			// Verification succeeded
			return true;
		}

		/// <summary>
		/// Gets the user info from the request and sets the <see cref="CurrentUser"/> value.
		/// If <see cref="CurrentUser"/> is already set, this function does nothing.
		/// </summary>
		/// <remarks>
		/// In case this returns false, the response value must be returned to the client.
		/// </remarks>
		/// <param name="request">The request from whom to take the user info.</param>
		/// <param name="response">A response JObject. In case of an error, this will be an error JSON. Otherwise always null.</param>
		/// <returns>True if no errors were encountered. Otherwise false.</returns>
		private bool GetUser(JObject request, out JObject response)
		{
			// Prevent redundant calls
			if (CurrentUser != null)
			{
				response = null;
				return true;
			}

			request.TryGetValue("username", out JToken username);
			request.TryGetValue("token", out JToken token);

			// Verify arguments
			var missing = new List<string>();
			if (username == null) missing.Add("username");
			if (token == null) missing.Add("token");

			if (missing.Any())
			{
				response = Templates.MissingArguments(missing.ToArray());
				return false;
			}

			// If the token can't be parsed, respond with 'InvalidArgument'
			if (!long.TryParse(token.ToString(), out long _))
			{
				response = Templates.InvalidArgument("token");
				return false;
			}

			// Get user, or if no user was found, respond with "InvalidLogin"
			CurrentUser = GetObject<User>(username, "username");
			if (CurrentUser == null)
			{
				response = Templates.InvalidLogin;
				return false;
			}

			// Verification succeeded
			response = null;
			return true;
		}
		#endregion

		/// <summary>
		/// Convenience method for returning single items from the database.
		/// </summary>
		/// <typeparam name="T">A subclass of <see cref="SchemaItem"/>, whose instances to return.</typeparam>
		/// <param name="value">The data to test <paramref name="column"/> against.</param>
		/// <param name="column">The table column whose values to test. Defaults to "id".</param>
		/// <param name="type">The type to represent the <paramref name="value"/> as. Defaults to String.</param>
		/// <returns></returns>
		protected T GetObject<T>(object value, string column = "id", MySqlDbType? type = null) where T : SchemaItem, new()
		{
			MySqlDbType operandtype = type ?? MySqlDbType.String;
			if (type == null && value is int)
				operandtype = MySqlDbType.Int64;

			return Connection.Select<T>(new MySqlConditionBuilder()
					.Column(column)
					.Equals(value, operandtype)
			).FirstOrDefault();
		}

		/// <summary>
		/// Pings the underlying connection.
		/// </summary>
		public bool Ping() => Connection.Ping();

		/// <summary>
		/// Disposes the underlying connection.
		/// </summary>
		public void Dispose() => Connection?.Dispose();
	}
}
