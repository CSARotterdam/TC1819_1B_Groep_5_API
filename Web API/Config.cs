using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Web_API {
	class Config {
		private static string defaultJson = @"{
			'databaseSettings': {
				'username': null,
				'password': null,
				'serverAddress': null
			},
			'connectionSettings': {
				'autodetect': true,
				'serverAddresses': [
					'localhost'
				]
			},
			'performanceSettings':{
				'workerThreadCount': 5,
			}
		}";

		public static dynamic loadConfig(){
			string filename = "config.json";
			dynamic Settings;

			if (!File.Exists(filename)){
				Settings = JObject.Parse(defaultJson);
				using (JsonTextWriter writer = new JsonTextWriter(File.CreateText(filename))) {
					writer.Formatting = Formatting.Indented;
					Settings.WriteTo(writer);
				}

			} else {
				Settings = JObject.Parse(File.ReadAllText(filename));
			}
			return Settings;
		}
	}
}
