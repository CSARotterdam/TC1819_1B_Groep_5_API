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
                Ping();

				//Wait a second!
				Thread.Sleep(1000);
			}
		}

        /// <summary>
        /// Returns True if the database connection is working, otherwise returns False.
        /// </summary>
        /// <returns></returns>
        public static bool Ping() {
            if (Program.wrapper.Ping()) {
                if (Program.ErrorCode == 1 && !Program.ManualError) {
                    Program.ErrorCode = 0;
                }
                return true;
            } else if (Program.ErrorCode == 0) {
                Program.ErrorCode = 1;
            }
            return false;
        }
	}
}
