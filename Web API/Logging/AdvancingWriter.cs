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
		/// Gets the time when this writer was created.
		/// </summary>
		public DateTime Creation { get; } = DateTime.Now;

		/// <summary>
		/// The duration of a file written by this writer before advancing a new file.
		/// </summary>
		public readonly TimeSpan Duration;
		private DateTime FileTimeStamp;

		private readonly Thread FileAdvancerThread;
		private TextWriter Stream;

		/// <summary>
		/// Gets or sets path of the files to create. This can be an unformatted string if <see cref="Compression"/>
		/// is set to true.
		/// </summary>
		/// <remarks>
		/// This path may include 3 string format items. The first specifies the time of it's creation.
		/// The third specifies the time this writer was created.
		/// The third specifies how many files have been written by this writer.
		/// </remarks>
		public string File
		{
			get
			{
				string getFile(int x) => string.Format(_file, FileTimeStamp, Creation, x);
				int i = 0;
				while (System.IO.File.Exists(getFile(i)))
				{
					string prevFile = getFile(i);
					i++;
					if (prevFile == getFile(i)) return prevFile;
				}
				return getFile(i);
			}
		}
		private readonly string _file;

		/// <summary>
		/// Gets or sets path of the compressed archives to create. Only used when <see cref="Compression"/> is set to true.
		/// </summary>
		/// <remarks>
		/// This path may include 3 string format items. The first specifies the time of it's creation.
		/// The third specifies the time this writer was created.
		/// The third specifies how many archives have been written by this writer.
		/// <para>
		/// Once set, this returns the same formatted string every time. Resets when setting the format again.
		/// </para>
		/// </remarks>
		public string Archive
		{
			get
			{
				if (_archive != null) return _archive;
				string getArchive(int x) => string.Format(archiveFormat, FileTimeStamp, Creation, x);
				int i = 0;
				while (System.IO.File.Exists(getArchive(i)))
				{
					string prevArchive = getArchive(i);
					i++;
					if (prevArchive == getArchive(i))
					{
						_archive = prevArchive;
						return _archive;
					}
				}
				_archive = getArchive(i);
				return _archive;
			}
			set
			{
				if (archiveFormat != null) return;
				archiveFormat = value;
				_archive = null;
				if (!System.IO.File.Exists(archiveFormat))
					System.IO.File.Create(Archive);
			}
		}
		private string archiveFormat;
		private string _archive;

		private bool Active = true;

		/// <summary>
		/// Gets or sets whether this <see cref="AdvancingWriter"/> compresses it's files after closing their streams.
		/// </summary>
		public bool Compression { get; set; } = false;
		/// <summary>
		/// Gets or sets whether this <see cref="AdvancingWriter"/> commpresses it's last file when closing or disposing.
		/// <para><see cref="Compression"/> must be true for this to have any effect.</para>
		/// </summary>
		public bool CompressOnClose { get; set; } = true;

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
				if (!Archives.Contains(archiveName) && System.IO.File.Exists(archiveName))
				{
					System.IO.File.Delete(archiveName);
					mode = ZipArchiveMode.Create;
				}

				using (var archive = ZipFile.Open(archiveName, mode))
				{
					var entry = archive.CreateEntryFromFile(
						file,
						$"{Path.GetFileNameWithoutExtension(file)}.{(mode == ZipArchiveMode.Update ? archive.Entries.Count : 0)}{Path.GetExtension(file)}"
					);
				}

				System.IO.File.Delete(file);
				Files.Remove(file);
				if (!Archives.Contains(archiveName))
					Archives.Add(archiveName);
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
			if (Compression && !CompressOnClose)
				Compression = false;
			Active = false;
			FileAdvancerThread.Interrupt();
			base.Dispose(disposing);
			FinalizeStream();
		}
	}
}