using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace API_Test_Client {
	public static class Data {
		public static Dictionary<string, string> requestTypes = new Dictionary<string, string>();
		public static void LoadFiles() {
			foreach(string file in Directory.GetFiles("Requests", "*.json", SearchOption.TopDirectoryOnly)) {
				string name = file.Split('\\')[1].Split('.')[0];
				string contents = File.ReadAllText(file);
				requestTypes.Add(name, contents);
			};
		}
	}
}
