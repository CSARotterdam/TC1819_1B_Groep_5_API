using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace API.Threads {
	class DatabaseMaintainer {
		public static void main(){
			while(true){
				//If ErrorCode = 1 (Database connection lost), continuously try to fix the connection
				if (Program.ErrorCode == 1) {
					Program.wrapper.Open();
				}

				//Ping the database server. If it fails, set error code to 1 unless another errorcode is already in effect.
				if(Program.wrapper.Ping()){
					if(Program.ErrorCode == 1 && !Program.ManualError) {
						Program.ErrorCode = 0;
					}
				} else if(Program.ErrorCode == 0){
					Program.ErrorCode = 1;
				}

				//Wait a second!
				Thread.Sleep(1000);
			}
		}
	}
}
