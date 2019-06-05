using MySQLWrapper.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace API.Commands
{
	static partial class CommandMethods
	{
		[MonitoringDescription("Uploads a single image. Requires a valid file argument.")]
		public static void UploadImage(params string[] args)
		{
			if (args.Length < 1)
			{
				log.Error("UploadImage requires one argument");
				return;
			}
			if (!File.Exists(args[0]))
			{
				log.Error($"{args[0]} is not a valid file path");
				return;
			}
			var image = new Image(args[0]);
			timer.Start();
			Connection.Upload(image);
			timer.Stop();
			log.Info($"({Math.Round(image.Data.Length / 1024 / timer.Elapsed.TotalSeconds, 2)} KiB/s) Uploaded {Path.GetFileName(args[0])}");
			timer.Reset();
		}

		[MonitoringDescription("Uploads all images from a folder. Requires a valid directory argument.")]
		public static void UploadImages(params string[] args)
		{
			if (args.Length < 1)
			{
				log.Error("UploadImages requires one argument");
				return;
			}
			if (!Directory.Exists(args[0]))
			{
				log.Error($"{args[0]} is not a valid directory");
				return;
			}
			var files = Directory.GetFiles(args[0]);
			int uploaded = 0;
			foreach (var file in files)
			{
				if (Image.ImageFormats.Contains(Path.GetExtension(file)))
				{
					try
					{
						UploadImage(file);
						uploaded++;
					}
					catch (Exception e)
					{
						timer.Stop();
						log.Error($"({Misc.FormatDelay(timer)}) {e.GetType().Name}: {e.Message}", false);
						timer.Reset();
					}
				}
			}
			log.Info($"Uploaded {uploaded}/{files.Length} file{(files.Length == 1 ? "" : "s")}.");
		}
	}
}
