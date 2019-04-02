using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Web_API {
	class DatabaseMaintainer {
		public static void main(){
			while(true){
				bool pingSuccess = Program.wrapper.Ping();
				if(pingSuccess){
					Program.ErrorState = false;
				} else {
					Program.ErrorState = true;
				}
				Thread.Sleep(1000);
			}
		}
	}
}
