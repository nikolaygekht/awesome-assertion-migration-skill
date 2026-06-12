using System;
using System.Collections.Generic;
using System.Linq;

namespace SensorFeed.Tests
{
    public class Reading
    {
        public DateTime At { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
    }

    public class ReadingWindow
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<Reading> Readings { get; set; } = new List<Reading>();

        public double Average() => Readings.Average(r => r.Value);
    }

    public class FeedException : Exception
    {
        public FeedException(string message)
            : base(message)
        {
        }
    }

    public class FeedClient
    {
        public ReadingWindow Fetch(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new FeedException("The requested range is empty or inverted");
            }

            var window = new ReadingWindow { Start = start, End = end };
            for (DateTime at = start; at < end; at = at.AddMinutes(30))
            {
                window.Readings.Add(new Reading
                {
                    At = at,
                    Value = 20.0 + 0.25 * window.Readings.Count,
                    Unit = "C",
                });
            }

            return window;
        }
    }
}
