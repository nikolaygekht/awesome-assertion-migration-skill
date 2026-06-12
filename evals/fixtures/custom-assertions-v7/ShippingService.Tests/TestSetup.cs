using System.Runtime.CompilerServices;
using FluentAssertions;
using FluentAssertions.Formatting;
using ShippingService.Tests.Assertions;

namespace ShippingService.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Formatter.AddFormatter(new ShipmentFormatter());
        AssertionOptions.EquivalencyPlan.Insert<TrackingCodeEquivalencyStep>();
    }
}
