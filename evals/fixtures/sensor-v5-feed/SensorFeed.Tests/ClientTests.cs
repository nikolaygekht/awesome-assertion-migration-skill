using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace SensorFeed.Tests
{
    public class ClientTests
    {
        [Fact]
        public void Inverted_range_is_rejected()
        {
            var client = new FeedClient();

            ((Action)(() => client.Fetch(21.March(2024).At(14, 00), 21.March(2024).At(10, 00))))
                .Should().Throw<FeedException>()
                .WithMessage("*range*");
        }

        [Fact]
        public void Valid_range_does_not_throw()
        {
            var client = new FeedClient();

            ((Action)(() => client.Fetch(21.March(2024), 22.March(2024)))).Should().NotThrow();
        }
    }
}
