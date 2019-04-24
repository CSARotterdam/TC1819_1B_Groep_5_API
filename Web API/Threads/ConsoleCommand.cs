using System;
using System.Collections.Generic;
using System.Text;

namespace API.Threads {
	class ConsoleCommand {
		public static void main(){
			while(true){
				string text = Console.ReadLine();
				string[] tokens = text.Split(" ");
				//TODO: comments lol
				switch(tokens[0]){
					case "errorcode":
						int ErrorCode;
						if(int.TryParse(tokens[1], out ErrorCode)){
							Program.ManualError = true;
							Program.ErrorCode = ErrorCode;
						} else if(tokens[1].ToLower() == "none"){
							Program.ManualError = false;
							Program.ErrorCode = 0;
						}
						break;
				}
			}
		}
	}
}
