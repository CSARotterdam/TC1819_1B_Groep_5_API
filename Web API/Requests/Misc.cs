using MySQLWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Requests {
	static partial class RequestMethods {
		public static TechlabMySQL wrapper;


		private static string generateUserToken(string username) {
			byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
			byte[] key = Guid.NewGuid().ToByteArray();
			string token = Convert.ToBase64String(time.Concat(key).ToArray());

			return token;
		}
	}
}
