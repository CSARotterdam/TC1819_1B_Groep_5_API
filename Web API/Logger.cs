using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Logging
{
	/// <summary>
	/// A class for creating logging levels.
	/// <para>This contains several default logging levels.</para>
	/// </summary>
	/// <seealso cref="DefaultLevels"/>
	public class Level
	{
		/// <summary>
		/// A collection of custom named <see cref="Level"/> instances.
		/// </summary>
		public static readonly Dictionary<string, Level> CustomLevels = new Dictionary<string, Level>();
		/// <summary>
		/// A collection of the default logging levels.
		/// </summary>
		public static readonly Dictionary<string, Level> DefaultLevels = new Dictionary<string, Level>();

		/// <summary>
		/// Disables all log messages.
		/// </summary>
		public static readonly Level OFF = new Level(int.MinValue, "OFF");
		/// <summary>
		/// Used for errors after which the program cannot continue running.
		/// </summary>
		public static readonly Level FATAL = new Level(-1000, "FATAL");
		/// <summary>
		/// Used for errors that nonetheless do not prevent the program from continuing.
		/// </summary>
		public static readonly Level ERROR = new Level(-100, "ERROR");
		/// <summary>
		/// Used to warn for things that may be out of the ordinary, but are otherwise not a problem.
		/// </summary>
		public static readonly Level WARN = new Level(-50, "WARN");
		/// <summary>
		/// Mostly used for debugging code.
		/// </summary>
		public static readonly Level DEBUG = new Level(0, "DEBUG");
		/// <summary>
		/// Used for general program information/feedback.
		/// </summary>
		public static readonly Level INFO = new Level(10, "INFO");
		/// <summary>
		/// Used for relatively fine logging. Not as fine as TRACE.
		/// </summary>
		public static readonly Level FINE = new Level(100, "FINE");
		/// <summary>
		/// Used for very fine information. E.G object construction, function calls, etc.
		/// </summary>
		public static readonly Level TRACE = new Level(1000, "TRACE");
		/// <summary>
		/// Enables all log messages.
		/// </summary>
		public static readonly Level ALL = new Level(int.MaxValue, "ALL");

		public string Name { get; }
		public int Value { get; }

		private Level(int value, string name, bool isDefault = true)
		{
			Value = value;
			Name = name;
			if (isDefault) DefaultLevels[name] = this;
		}

		/// <summary>
		/// Creates a new logging <see cref="Level"/> objects.
		/// </summary>
		/// <param name="value">The logging level value. This must be a unique value.</param>
		/// <param name="name">The name of the logging level. This is case sensitive and must be unique.</param>
		/// <returns>The newly created <see cref="Level"/> instance.</returns>
		/// <exception cref="ArgumentException">When <paramref name="name"/> or <paramref name="value"/> are not unique.</exception>
		public static Level NewCustomLevel(int value, string name)
		{
			if (CustomLevels.ContainsKey(name))
				throw new ArgumentException($"Duplicate level '{name}'.", "name");
			if (CustomLevels.Any(x => x.Value.Value == value))
				throw new ArgumentException($"Duplicate level {value}.", "value");
			CustomLevels[name] = new Level(value, name, false);
			return CustomLevels[name];
		}

		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		public override string ToString() => Name;
	}

	/// <summary>
	/// General purpose logging class.
	/// <para>
	/// These loggers support a hierarchy structure of <see cref="Logger"/> objects,
	/// where one parent logger passes its log messages to any child loggers.
	/// </para>
	/// </summary>
	class Logger : IDisposable
	{
		/// <summary>
		/// A read-only collection of associated loggers
		/// </summary>
		public IReadOnlyCollection<Logger> Children => _children.AsReadOnly();
		private List<Logger> _children = new List<Logger>();

		/// <summary>
		/// A read-only collection of loggers this object is associated with.
		/// </summary>
		public IReadOnlyCollection<Logger> Parents => _parents.AsReadOnly();
		private List<Logger> _parents = new List<Logger>();

		/// <summary>
		/// The current logging level. This can be changed at any time.
		/// </summary>
		public Level LogLevel { get; set; }
		/// <summary>
		/// The collection of <see cref="TextWriter"/> objects this logger writes to.
		/// </summary>
		/// <remarks>
		/// When removing streams, make sure to close and/or dispose them, as this does not happen automatically.
		/// </remarks>
		public List<TextWriter> OutputStreams { get; } = new List<TextWriter>();
		/// <summary>
		/// Disables logging for this instance and without changing the logging level.
		/// <para>This also prevents writing to child loggers, but it does not silence it's children.</para>
		/// </summary>
		public bool Silent { get; set; } = false;

		/// <summary>
		/// The dateTime format for the logging timestamps. This can be changed at any time.
		/// </summary>
		public string TimeFormat { get; set; } = "[H:mm:ss]";

		/// <summary>
		/// Creates a new instance of <see cref="Logger"/>.
		/// </summary>
		/// <remarks>
		/// This constructor supports custom log levels.
		/// </remarks>
		/// <param name="level">The maximum logging level, represented as <see cref="int"/>.</param>
		/// <param name="outStreams">A collection of unique <see cref="TextWriter"/> objects.</param>
		public Logger(Level level, params TextWriter[] outStreams)
		{
			LogLevel = level;
			foreach (var stream in outStreams)
				OutputStreams.Add(stream);
			Fine("Started logging");
		}

		/// <summary>
		/// Writes a message with the FATAL log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Fatal(object message) => Write(Level.FATAL, message);
		/// <summary>
		/// Writes a message with the FATAL log level. This includes an exception traceback.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="innerException">The exception that caused this message.</param>
		public void Fatal(object message, Exception innerException) => Write(Level.FATAL, $"{message}\n{innerException}");

		/// <summary>
		/// Writes a message with the ERROR log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Error(object message) => Write(Level.ERROR, message);
		/// <summary>
		/// Writes a message with the ERROR log level. This includes an exception traceback.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="innerException">The exception that caused this message.</param>
		public void Error(object message, Exception innerException) => Write(Level.ERROR, $"{message}\n{innerException}");

		/// <summary>
		/// Writes a message with the WARN log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Warning(object message) => Write(Level.WARN, message);
		/// <summary>
		/// Writes a message with the INFO log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Info(object message) => Write(Level.INFO, message);
		/// <summary>
		/// Writes a message with the DEBUG log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Debug(object message) => Write(Level.DEBUG, message);
		/// <summary>
		/// Writes a message with the FINE log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Fine(object message) => Write(Level.FINE, message);
		/// <summary>
		/// Writes a message with the TRACE log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		public void Trace(object message) => Write(Level.TRACE, message);

		/// <summary>
		/// Writes the log to the output streams if the level is lower or equal to the set logging level.
		/// </summary>
		/// <param name="level">A <see cref="Level"/> message level.</param>
		/// <param name="message">The value to write.</param>
		public void Write(Level level, object message)
		{
			if (Silent) return;
			foreach (var logger in Children) logger.Write(level, message);
			if (disposedValue) throw new ObjectDisposedException(ToString());
			if (LogLevel.Value >= level.Value)
				foreach (var stream in OutputStreams)
					stream.WriteLine($"[{level}] {DateTime.Now.ToString(TimeFormat)} {message}");
		}

		/// <summary>
		/// Adds a new child logger to this logger.
		/// <para>All logging messages to this logger will be passed on to its child loggers.</para>
		/// </summary>
		/// <param name="logger">The <see cref="Logger"/> object to add to this logger.</param>
		public void Add(Logger logger)
		{
			_children.Add(logger);
			logger._parents.Add(this);
		}

		/// <summary>
		/// Removes a logger from this object's children.
		/// </summary>
		/// <param name="logger">The logger to remove.</param>
		public void Remove(Logger logger)
		{
			_children.Remove(logger);
			logger._parents.Remove(this);
		}

		/// <summary>
		/// Closes the associated <see cref="TextWriter"/> objects and <see cref="Logger"/> children.
		/// </summary>
		public void Close()
		{
			foreach (var logger in Children)
			{
				logger.Close();
				_children.Remove(logger);
				foreach (var parent in Parents) parent.Remove(this);
			}
			Fine("Closing log...");
			foreach (var stream in OutputStreams) stream.Close();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					foreach (var logger in Children)
					{
						logger.Dispose();
						_children.Remove(logger);
					}
					Fine("Disposing log...");
					foreach (var stream in OutputStreams)
						stream.Dispose();
					foreach (var parent in Parents) parent.Remove(this);
				}

				disposedValue = true;
			}
		}

		/// <summary>
		/// Disposes the current 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}
