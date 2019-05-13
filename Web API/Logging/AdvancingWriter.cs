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

		private Thread FileAdvancerThread;
		private TextWriter Stream;
		private string File
		{
			get
			{
				return string.Format(_file, FileTimeStamp, _Files.Count);
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
				return string.Format(_Archive, FileTimeStamp, _Files.Count);
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
		/// <summary>
		/// An array of files that this <see cref="AdvancingWriter"/> has created. Compressed archives replace their uncompressed counterparts.
		/// </summary>
		public string[] Files => _Files.ToArray();
		private List<string> _Files = new List<string>();

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
			Stream = new StreamWriter(File) { AutoFlush = true };
			_Files.Add(File);
			FileAdvancerThread = new Thread(new ThreadStart(AdvanceFile));
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
					_Files.Add(File);
				}
			}
		}

		private void FinalizeStream()
		{
			lock (Stream)
			{
				Stream.Dispose();
				if (!Compression) return;
				var file = _Files.Last();
				var archiveName = Archive ?? Path.ChangeExtension(file, "zip");
				using (var archive = ZipFile.Open(archiveName, ZipArchiveMode.Create))
					archive.CreateEntryFromFile(file, Path.GetFileName(file));
				_Files[_Files.Count - 1] = archiveName;
				System.IO.File.Delete(file);
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