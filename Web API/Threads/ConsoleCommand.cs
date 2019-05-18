using Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Threads {
	class ConsoleCommand {
		public static void main(Logger log) {
			log.Info("Thread ConsoleCommands now running.");
			while(true){
				string text = Console.ReadLine();
				string[] tokens = text.Split(" ");


				switch (tokens[0]){

				}
			}
		}
	}
}
