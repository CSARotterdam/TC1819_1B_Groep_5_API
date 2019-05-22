using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.IO.Compression;
using System.Linq;

namespace Logging
{
	class AdvancingWriter : TextWriter
	{
		public override Encoding Encoding => Encoding.UTF8;

		/// <summary>
		/// The duration of a file written by this writer before advancing a new file.
		/// </summary>
		public readonly TimeSpan Duration;
		private DateTime FileTimeStamp;

		private readonly Thread FileAdvancerThread;
		private TextWriter Stream;
		private string File
		{
			get
			{
				return string.Format(_file, FileTimeStamp, Files.Count);
			}
		}
		private readonly string _file;
		/// <summary>
		/// The path of the compressed archives to create. Only used when <see cref="Compression"/> is set to true.
		/// <para>This path may include 2 string format items. The first specifies the time of it's creation; The
		/// second specifies how many files have been written by this writer.</para>
		/// </summary>
		public string Archive
		{
			private get
			{
				return string.Format(_Archive, FileTimeStamp, Files.Count);
			}
			set
			{
				if (_Archive != null) return;
				_Archive = value;
			}
		}
		private string _Archive;

		private bool Active = true;

		/// <summary>
		/// Gets or sets whether this <see cref="AdvancingWriter"/> compresses it's files after closing their streams.
		/// </summary>
		public bool Compression { get; set; } = false;

		private readonly List<string> Files = new List<string>();
		private readonly List<string> Archives = new List<string>();

		/// <summary>
		/// Creates a new instance of <see cref="AdvancingWriter"/> that advances it's file at midnight.
		/// </summary>
		/// <param name="file">The path of the file to write. This may include 2 formatting items. (See summary)</param>
		public AdvancingWriter(string file) : this(file, new TimeSpan(24, 0, 0))
		{ }
		/// <summary>
		/// Creates a new instance of <see cref="AdvancingWriter"/> that advances it's file after a given period of time,
		/// starting from the time this instance was created.
		/// </summary>
		/// <param name="file">The path of the file to write. This may include 2 formatting items. (See summary)</param>
		/// <param name="duration">The timespan between consecutive files.</param>
		public AdvancingWriter(string file, TimeSpan duration) : this(file, DateTime.Now.TimeOfDay, duration)
		{ }
		/// <summary>
		/// Creates a new instance of <see cref="AdvancingWriter"/> that advances it's file after a given period of time.
		/// <para>The first advancement will happen at &lt;<paramref name="timeOfDay"/>&gt;, afther which all advancements
		/// happen every &lt;<paramref name="duration"/>&gt;.</para>
		/// </summary>
		/// <param name="file">The path of the file to write. This may include 2 formatting items. (See summary)</param>
		public AdvancingWriter(string file, TimeSpan timeOfDay, TimeSpan duration)
		{
			_file = file;
			Duration = duration;
			FileTimeStamp = DateTime.Now.Date.Add(timeOfDay);
			if (FileTimeStamp <= DateTime.Now)
				FileTimeStamp += (int)((DateTime.Now - FileTimeStamp) / Duration + 1) * Duration;

			// in case of overwriting, reset the creation time
			if (System.IO.File.Exists(File)) System.IO.File.SetCreationTime(File, FileTimeStamp);

			Stream = new StreamWriter(File) { AutoFlush = true };
			Files.Add(File);
			FileAdvancerThread = new Thread(new ThreadStart(AdvanceFile)) { Name = "FileAdvancerThread" };
			FileAdvancerThread.Start();
		}

		private void AdvanceFile()
		{
			while (Active)
			{
				var delay = FileTimeStamp.Subtract(DateTime.Now);

				try
				{ Thread.Sleep(delay); }
				catch (ThreadInterruptedException)
				{ break; }

				lock (Stream)
				{
					FinalizeStream();
					FileTimeStamp += Duration;
					Stream = new StreamWriter(File) { AutoFlush = true };
					if (!Files.Contains(File))
						Files.Add(File);
				}
			}
		}

		private void FinalizeStream()
		{
			lock (Stream)
			{
				Stream.Dispose();
				if (!Compression) return;

				var file = Files.Last();
				var archiveName = Archive ?? Path.ChangeExtension(file, "zip");

				var mode = ZipArchiveMode.Update;
				if (!Files.Contains(archiveName) && System.IO.File.Exists(archiveName))
				{
					System.IO.File.Delete(archiveName);
					mode = ZipArchiveMode.Create;
				}

				using (var archive = ZipFile.Open(archiveName, mode))
				{
					archive.CreateEntryFromFile(
						file,
						$"{Path.GetFileNameWithoutExtension(file)}.{(mode == ZipArchiveMode.Update ? archive.Entries.Count : 0)}{Path.GetExtension(file)}"
					);
				}

				System.IO.File.Delete(file);
				Files.Remove(file);
				if (!Files.Contains(archiveName))
					Files.Add(archiveName);
			}
		}

		public override void Flush()
		{
			lock (Stream) Stream.Flush();
		}

		public override void Close()
		{
			lock (Stream)
			{
				Stream.Close();
				Dispose(true);
			}
		}

		public override void Write(char value)
		{
			lock (Stream) Stream.Write(value);
		}

		public override void Write(string value)
		{
			lock (Stream) Stream.Write(value);
		}

		public override void WriteLine(string value)
		{
			lock (Stream) Stream.WriteLine(value);
		}

		protected override void Dispose(bool disposing)
		{
			Active = false;
			FileAdvancerThread.Interrupt();
			base.Dispose(disposing);
			FinalizeStream();
		}
	}
}