using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace API.Threads {
	class DatabaseMaintainer {
		public static void main(){
			while(true){
				//TODO: DOCUMENTATION, blaasinstrument (lol)
				if (Program.ErrorCode == 1) {
					Program.wrapper.Open();
				}
				if(Program.wrapper.Ping()){
					if(Program.ErrorCode == 1 && !Program.ManualError) {
						Program.ErrorCode = 0;
					}
				} else if(Program.ErrorCode == 0){
					Program.ErrorCode = 1;
				}
				Thread.Sleep(1000);
			}
		}
	}
}
