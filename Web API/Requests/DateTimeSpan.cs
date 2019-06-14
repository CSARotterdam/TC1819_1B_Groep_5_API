using System;

namespace API.Requests
{
	/// <summary>
	/// A class representing a timespan between 2 points in time.
	/// </summary>
	public class DateTimeSpan
	{
		/// <summary>
		/// Gets or sets the start datetime.
		/// <para>
		/// If value is larger than <see cref="End"/>, Start and End are swapped in order to keep the
		/// time difference non-negative.
		/// </para>
		/// </summary>
		public DateTime Start
		{
			set
			{
				if (End < value)
				{
					start = End;
					End = value;
				}
				else start = value;
			}
			get
			{
				return start;
			}
		}
		private DateTime start;
		/// <summary>
		/// Gets or sets the end datetime.
		/// <para>
		/// If value is larger than <see cref="Start"/>, Start and End are swapped in order to keep the
		/// time difference non-negative.
		/// </para>
		/// </summary>
		public DateTime End
		{
			set
			{
				if (value < Start)
				{
					end = Start;
					Start = value;
				}
				else end = value;
			}
			get
			{
				return end;
			}
		}
		private DateTime end;

		/// <summary>
		/// Gets the time between <see cref="Start"/> and <see cref="End"/>.
		/// </summary>
		public TimeSpan Duration => End - Start;

		/// <summary>
		/// Creates a new instance of <see cref="DateTimeSpan"/>.
		/// </summary>
		/// <remarks>
		/// If <paramref name="end"/> is lower than <paramref name="start"/>, the values are swapped to
		/// maintain a non-negative time difference.
		/// </remarks>
		/// <param name="start">The point in time where this DateTimeSpan starts.</param>
		/// <param name="end">The point in time where this DateTimeSpan ends.</param>
		public DateTimeSpan(DateTime start, DateTime end)
		{
			End = end;
			Start = start;
		}

		/// <summary>
		/// Returns whether or not a <see cref="DateTime"/> falls within this <see cref="DateTimeSpan"/>.
		/// </summary>
		/// <param name="dateTime">The datetime object to test agains this DateTimeSpan.</param>
		public bool Contains(DateTime dateTime) => Start < dateTime && End > dateTime;

		/// <summary>
		/// Returns whether or not another <see cref="DateTimeSpan"/> in any way overlaps this
		/// <see cref="DateTimeSpan"/>.
		/// </summary>
		/// <param name="span">The DateTimeSpan to test for any overlaps.</param>
		public bool Overlaps(DateTimeSpan span)
		{
			return span.Equals(this)
				|| (span.Start < End && span.End == End)
				|| (span.End > Start && span.Start == Start)
				|| (span.Contains(Start) && Contains(span.End))
				|| (span.Contains(End) && Contains(span.Start))
				|| (span.Contains(Start) && span.Contains(End))
				|| (Contains(span.Start) && Contains(span.End));
		}

		/// <summary>
		/// Returns whether or not a date range in any way overlaps this
		/// <see cref="DateTimeSpan"/>.
		/// </summary>
		/// <remarks>
		/// Equivalent to Overlaps(new DateTimeSpan(start, end));
		/// </remarks>
		/// <param name="start">The start of the date span to check.</param>
		/// <param name="end">The end of the date span to check.</param>
		public bool Overlaps(DateTime start, DateTime end) => Overlaps(new DateTimeSpan(start, end));

		public bool Equals(DateTimeSpan span) => span.Start == Start && span.End == End;

		public override string ToString() => $"{Start.ToString()} - {End.ToString()}";

		public string ToString(string format) => $"{Start.ToString(format)} - {End.ToString(format)}";
	}
}