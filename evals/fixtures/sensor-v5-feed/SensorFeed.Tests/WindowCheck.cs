using FluentAssertions.Execution;

namespace SensorFeed.Tests
{
    /// <summary>
    /// Plain assertion wrapper (not derived from any FluentAssertions base class)
    /// used by several test classes to validate a fetched window in one call.
    /// </summary>
    public class WindowCheck
    {
        private readonly ReadingWindow window;

        public WindowCheck(ReadingWindow window)
        {
            this.window = window;
        }

        public WindowCheck BeComplete(string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .ForCondition(window != null)
                .FailWith("Expected the window to be fetched, but it is <null>.")
                .Then
                .ForCondition(window.Readings.Count > 0)
                .FailWith("Expected the window to contain readings, but it is empty.")
                .Then
                .ForCondition(window.End > window.Start)
                .FailWith("Expected the window range to be positive, but it goes from {0} to {1}.",
                    window.Start, window.End);

            return this;
        }

        public WindowCheck UseUnit(string unit, string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .ForCondition(window.Readings.TrueForAll(r => r.Unit == unit))
                .FailWith("Expected every reading to use unit {0}{reason}, but some do not.", unit);

            return this;
        }
    }
}
