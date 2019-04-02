using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Web_API {
	class DatabaseMaintainer {
		public static void main(){
			while(true){
				bool pingSuccess = Program.wrapper.Ping();
				if (pingSuccess){
					if(Program.ErrorCode == 1 && !Program.ManualError){
						Program.ErrorCode = 0;
					}
				} else {
					Program.ErrorCode = 1;
				}
				Thread.Sleep(1000);
			}
		}
	}
}
