using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace API.Threads {
    class Logging {
        public static void main(Logger log, Logger child) {

        }

        public static void compressLogs() {
            //Create filename
            String[] files = Directory.GetFiles("Logs", DateTime.Today.ToShortDateString() + "-*.zip");
            String filename = DateTime.Today.ToShortDateString() + "-" + (files.GetLength(0) + 1).ToString();

            //Create archive
            using (var zip = ZipFile.Open("Logs\\" + filename + ".zip", ZipArchiveMode.Create))
                zip.CreateEntryFromFile("Logs\\latest.log", filename+".log");
        }
    }
}
