using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace SensorFeed.Tests
{
    public class WindowTests
    {
        private static readonly DateTime WindowStart = 21.March(2024).At(10, 00);
        private static readonly DateTime WindowEnd = 21.March(2024).At(14, 00);

        [Fact]
        public void Fetched_window_covers_the_requested_range()
        {
            var client = new FeedClient();

            var window = client.Fetch(WindowStart, WindowEnd);

            window.Start.Should().BeOnOrAfter(21.March(2024));
            window.End.Should().BeOnOrBefore(22.March(2024));
            window.Readings.First().At.Should().BeOnOrAfter(WindowStart).And.BeBefore(WindowEnd);
            window.Readings.Last().At.Should().BeAfter(WindowStart);
        }

        [Fact]
        public void Reading_count_matches_the_sampling_interval()
        {
            var client = new FeedClient();

            var window = client.Fetch(WindowStart, WindowEnd);

            window.Readings.Count.Should().BeGreaterOrEqualTo(8);
            window.Readings.Count.Should().BeLessOrEqualTo(8);
        }

        [Fact]
        public void Average_temperature_is_close_to_the_seeded_curve()
        {
            var client = new FeedClient();

            var window = client.Fetch(WindowStart, WindowEnd);

            window.Average().Should().BeApproximately(20.875, 0.001);
            window.Readings.First().Value.Should().BeApproximately(20.0, 1e-9);
        }

        [Fact]
        public void All_readings_use_celsius()
        {
            var client = new FeedClient();

            var window = client.Fetch(WindowStart, WindowEnd);

            window.Readings.Should().OnlyContain(r => r.Unit == "C");
            window.Readings.Should().NotBeNullOrEmpty();

            new WindowCheck(window)
                .BeComplete("the feed always returns data for a valid range")
                .UseUnit("C");
        }
    }
}
