using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

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

		#region Default Levels
		/// <summary>
		/// Disables all log messages.
		/// </summary>
		public static readonly Level OFF = new Level(int.MinValue, "OFF", true);
		/// <summary>
		/// Mostly used for debugging code. 
		/// </summary>
		public static readonly Level DEBUG = new Level(-1000, "DEBUG", true);
		/// <summary>
		/// Used for errors after which the program cannot continue running.
		/// </summary>
		public static readonly Level FATAL = new Level(-500, "FATAL", true);
		/// <summary>
		/// Used for errors that nonetheless do not prevent the program from continuing.
		/// </summary>
		public static readonly Level ERROR = new Level(-250, "ERROR", true);
		/// <summary>
		/// Used to warn for things that may be out of the ordinary, but are otherwise not a problem.
		/// </summary>
		public static readonly Level WARN = new Level(-100, "WARN", true);
		/// <summary>
		/// Used for general program information/feedback.
		/// </summary>
		public static readonly Level INFO = new Level(0, "INFO", true);
		/// <summary>
		/// Used for logs that document program configuration events.
		/// </summary>
		public static readonly Level CONFIG = new Level(250, "CONFIG", true);
		/// <summary>
		/// Used for relatively fine logging. Not as fine as TRACE.
		/// </summary>
		public static readonly Level FINE = new Level(500, "FINE", true);
		/// <summary>
		/// Used for very fine information. E.G object construction, function calls, etc.
		/// </summary>
		public static readonly Level TRACE = new Level(1000, "TRACE", true);
		/// <summary>
		/// Enables all log messages.
		/// </summary>
		public static readonly Level ALL = new Level(int.MaxValue, "ALL", true);
		#endregion

		public string Name { get; }
		public int Value { get; }

		private Level(int value, string name, bool isDefault)
		{
			Value = value;
			Name = name;
			if (isDefault) DefaultLevels[name] = this;
		}

		/// <summary>
		/// Creates a new instance of <see cref="Level"/>.
		/// </summary>
		/// <param name="value">The logging level value. This must be a unique value.</param>
		/// <param name="name">The name of the logging level. This is case sensitive and must be unique.</param>
		/// <returns>The newly created <see cref="Level"/> instance.</returns>
		/// <exception cref="ArgumentException">When <paramref name="name"/> or <paramref name="value"/> are not unique.</exception>
		public Level(int value, string name) : this(value, name, false)
		{
			CustomLevels[name] = this;
		}

		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		public override string ToString() => Name;
	}

	/// <summary>
	/// General purpose logging class, influenced largely by the logging class in Python.
	/// <para>
	/// These loggers support a hierarchy structure of <see cref="Logger"/> objects,
	/// where one parent logger passes its log messages to any child loggers.
	/// </para>
	/// </summary>
	class Logger : IDisposable
	{
		/// <summary>
		/// The name of this logger object
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// The time when this logger was created
		/// </summary>
		public DateTime Created { get; } = DateTime.Now;

		/// <summary>
		/// A read-only collection of associated loggers
		/// </summary>
		public IReadOnlyCollection<Logger> Children => _children.AsReadOnly();
		private readonly List<Logger> _children = new List<Logger>();

		/// <summary>
		/// A read-only collection of loggers this object is associated with.
		/// </summary>
		public IReadOnlyCollection<Logger> Parents => _parents.AsReadOnly();
		private readonly List<Logger> _parents = new List<Logger>();

		/// <summary>
		/// The collection of <see cref="TextWriter"/> objects this logger writes to.
		/// </summary>
		/// <remarks>
		/// When removing streams, make sure to close and/or dispose them, as this does not happen automatically.
		/// </remarks>
		public List<TextWriter> OutputStreams { get; } = new List<TextWriter>();

		/// <summary>
		/// The current logging level. This can be changed at any time.
		/// </summary>
		public Level LogLevel { get; set; }
		
		/// <summary>
		/// Gets or sets the format used for log records.
		/// </summary>
		public string Format { get; set; } = "{asctime:HH:mm:ss} {classname,-15} {levelname,6}: {message}";
		/// <summary>
		/// Sets whether or not the log record stacktraces will use file info.
		/// </summary>
		public bool UseFileInfo { get; set; } = true;

		/// <summary>
		/// Disables logging for this instance and without changing the logging level.
		/// <para>This also prevents writing to child loggers, but it does not silence it's children.</para>
		/// </summary>
		public bool Silent { get; set; } = false;

		/// <summary>
		/// Creates a new instance of <see cref="Logger"/>.
		/// </summary>
		/// <remarks>
		/// This constructor supports custom log levels.
		/// </remarks>
		/// <param name="level">The maximum logging level, represented as <see cref="int"/>.</param>
		/// <param name="outStreams">A collection of unique <see cref="TextWriter"/> objects.</param>
		public Logger(Level level, params TextWriter[] outStreams)
			: this(level, null, outStreams)
		{ }
		/// <summary>
		/// Creates a new instance of <see cref="Logger"/> with a custom name.
		/// </summary>
		/// <remarks>
		/// This constructor supports custom log levels.
		/// </remarks>
		/// <param name="level">The maximum logging level, represented as <see cref="int"/>.</param>
		/// <param name="name">The name of the new logger. If null, a default name with the classname and hashcode will be chosen.</param>
		/// <param name="outStreams">A collection of unique <see cref="TextWriter"/> objects.</param>
		public Logger(Level level, string name, params TextWriter[] outStreams)
			: this(level, name, null, outStreams)
		{ }
		/// <summary>
		/// Creates a new instance of <see cref="Logger"/> with a custom name and format.
		/// </summary>
		/// <remarks>
		/// This constructor supports custom log levels.
		/// </remarks>
		/// <param name="level">The maximum logging level, represented as <see cref="int"/>.</param>
		/// <param name="name">The name of the new logger. If null, a default name with the classname and hashcode will be chosen.</param>
		/// <param name="format">The format used for log records. If null, the default format is used.</param>
		/// <param name="outStreams">A collection of unique <see cref="TextWriter"/> objects.</param>
		public Logger(Level level, string name, string format, params TextWriter[] outStreams)
		{
			LogLevel = level;
			foreach (var stream in outStreams)
				OutputStreams.Add(stream);
			Name = name ?? $"{GetType().Name}@{GetHashCode().ToString("x")}";
			Format = format ?? Format;
			Fine("Started log");
		}

		#region Default Logging Methods
		/// <summary>
		/// Writes a message with the DEBUG log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Debug(object message, bool stackTrace = false) => Write(Level.DEBUG, message, stackTrace);

		/// <summary>
		/// Writes a message with the FATAL log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Fatal(object message, bool stackTrace = false) => Write(Level.FATAL, message, stackTrace);
		/// <summary>
		/// Writes a message with the FATAL log level. This includes an exception traceback.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="cause">The exception that caused this message.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Fatal(object message, Exception cause, bool stackTrace = true) => Write(Level.FATAL, message, new StackTrace(cause, UseFileInfo), stackTrace);

		/// <summary>
		/// Writes a message with the ERROR log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Error(object message, bool stackTrace = false) => Write(Level.ERROR, message, stackTrace);
		/// <summary>
		/// Writes a message with the ERROR log level. This includes an exception traceback.
		/// <para>The traceback of the given exception will be used, where the most recent calls will end at the cause.</para>
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="cause">The exception that caused this message. This exception's traceback will be used.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Error(object message, Exception cause, bool stackTrace = true) => Write(Level.ERROR, message, new StackTrace(cause, UseFileInfo), stackTrace);

		/// <summary>
		/// Writes a message with the WARN log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Warning(object message, bool stackTrace = false) => Write(Level.WARN, message, stackTrace);

		/// <summary>
		/// Writes a message with the INFO log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Info(object message, bool stackTrace = false) => Write(Level.INFO, message, stackTrace);

		/// <summary>
		/// Writes a message with the CONFIG log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Config(object message, bool stackTrace = false) => Write(Level.CONFIG, message, stackTrace);

		/// <summary>
		/// Writes a message with the FINE log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Fine(object message, bool stackTrace = false) => Write(Level.FINE, message, stackTrace);

		/// <summary>
		/// Writes a message with the TRACE log level.
		/// </summary>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Trace(object message, bool stackTrace = false) => Write(Level.TRACE, message, stackTrace);
		#endregion

		/// <summary>
		/// Writes the log to the output streams if the level is lower or equal to the set logging level.
		/// </summary>
		/// <param name="level">A <see cref="Level"/> message level.</param>
		/// <param name="message">The value to write.</param>
		/// <param name="stackTrace">Set whether to include a full stacktrace in the log record.</param>
		public void Write(Level level, object message, bool stackTrace = false) => Write(level, message, new StackTrace(UseFileInfo), stackTrace);

		/// <summary>
		/// Writes the log to the output streams if the level is lower or equal to the set logging level.
		/// <para>This function is thread-safe due to it's stream locking.</para>
		/// </summary>
		/// <param name="level">A <see cref="Level"/> message level.</param>
		/// <param name="message">The value to write.</param>
		/// <param name="stack">The stacktrace to reference in the log record.</param>
		/// <param name="includeStackTrace">Set whether to include a full stacktrace in the log record.</param>
		private void Write(Level level, object message, StackTrace stack, bool includeStackTrace)
		{
			if (Silent) return;
			if (disposedValue) throw new ObjectDisposedException(ToString());

			foreach (var logger in Children) logger.Write(level, message, stack, includeStackTrace);
			if (LogLevel.Value < level.Value) return;

			foreach (var stream in OutputStreams)
			{
				lock (stream)
				{
					stream.WriteLine(GetRecord(level, message.ToString(), stack, includeStackTrace));
					stream.Flush();
				}
			}
		}

		/// <summary>
		/// Adds a new child logger to this logger.
		/// <para>All logging messages to this logger will be passed on to its child loggers.</para>
		/// </summary>
		/// <param name="logger">The <see cref="Logger"/> object to add to this logger.</param>
		public void Attach(Logger logger)
		{
			_children.Add(logger);
			logger._parents.Add(this);
		}

		/// <summary>
		/// Removes a logger from this object's children.
		/// </summary>
		/// <param name="logger">The logger to remove.</param>
		public void Detach(Logger logger)
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
				foreach (var parent in Parents) parent.Detach(this);
			}
			Fine("Closing log...");
			foreach (var stream in OutputStreams) stream.Close();
		}

		/// <summary>
		/// Returns a formatted log record based on this logger instance.
		/// </summary>
		/// <remarks>
		/// TL;DR - Big dumb block of code that fills in the blanks. Replaces and formats stuff. Makes the log look nice.
		/// </remarks>
		/// <param name="msg">The string to format.</param>
		protected string GetRecord(Level level, string message, StackTrace stack, bool includeStackTrace)
		{
			// check if stackinfo has been specified. if not, stack info will always be appended.
			bool appendStackTrace = !Regex.IsMatch(Format, ".*{stackinfo.*", RegexOptions.IgnoreCase);

			// replace all available attributes for records with a number for string formatting.
			string format = Format;
			foreach (var value in Enum.GetValues(typeof(RecordAttributes)).Cast<int>().Reverse())
			{
				var name = Enum.GetName(typeof(RecordAttributes), value);
				format = Regex.Replace(format, '{' + name, '{' + value.ToString(), RegexOptions.IgnoreCase);
			}
			// prepare local fields extracted from the stacktrace
			Type callerType = null;
			MethodBase callerFunc = null;
			int? lineno = null;
			string fileName = null;
			string stackTrace = stack.ToString();

			// loops through all stacktrace frames and fills in the previous values
			int i = 0;
			foreach (var frame in stack.GetFrames())
			{
				i++;
				callerFunc = frame.GetMethod();
				fileName = frame.GetFileName();
				lineno = frame.GetFileLineNumber();
				if (i != stack.FrameCount && callerType == GetType() && callerFunc.DeclaringType == GetType() && stack.GetFrame(i).GetMethod().DeclaringType == GetType())
				{
					callerFunc = stack.GetFrame(i).GetMethod();
					break; // Internal logging calls (log calls from the Logger class) only go through the default logging level methods,
						   // meaning once 3 calls originating from Logger have been found, we can be sure the call actually came from the Logger class.
				}
				callerType = callerFunc.DeclaringType;
				if (callerType != GetType())
					break;
			}
			if (i <= stack.FrameCount)
			{
				// trim chained logging calls from the stacktrace. These calls are after the first logging call and are irrelevant.
				var stacklines = stackTrace.Split("\r\n");
				stackTrace = stacklines[i-1];
                for (; i < stacklines.Length; i++)
                    stackTrace += "\r\n" + stacklines[i];
			}

			// format all attributes 
			return string.Format(format,
				DateTime.Now,
				DateTimeOffset.Now.ToUnixTimeMilliseconds()/1000d,
				callerType.Name,
				Path.GetFileName(fileName),
				callerFunc.Name,
				level,
				level.Value,
				lineno,
				message,
				callerFunc.Module,
				Name,
				fileName,
				Process.GetCurrentProcess().Id,
				Process.GetCurrentProcess().ProcessName,
				DateTime.Now - Process.GetCurrentProcess().StartTime,
				includeStackTrace ? "\n" + stackTrace.ToString().Remove(stackTrace.ToString().Length - 2, 2).Remove(0, 0) : null,
				Thread.CurrentThread.ManagedThreadId,
				Thread.CurrentThread.Name
			) + (appendStackTrace && includeStackTrace ? "\n" + stackTrace.ToString().Remove(stackTrace.ToString().Length - 2, 2) : null);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// New list copy because otherwise we are changing the list we are iterating through
					foreach (var logger in new List<Logger>(Children))
						logger.Dispose();
					Fine("Disposing log...");
					foreach (var stream in OutputStreams)
						stream.Dispose();
					// Same reason for new list as previous
					foreach (var parent in new List<Logger>(Parents)) parent.Detach(this);
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

		/// <summary>
		/// An enum of attributes used in log records
		/// </summary>
		enum RecordAttributes
		{
			// TODO: Add documentation (wow im so exited)
			asctime,
			created,
			className,
			fileName,
			funcName,
			levelName,
			levelno,
			lineno,
			message,
			module,
			name,
			pathname,
			processId,
			processName,
			relativeCreated,
			stackInfo,
			threadId,
			threadName
		}
	}
}