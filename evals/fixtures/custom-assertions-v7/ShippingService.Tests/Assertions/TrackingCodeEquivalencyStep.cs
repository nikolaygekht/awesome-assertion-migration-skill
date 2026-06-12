using FluentAssertions;
using FluentAssertions.Equivalency;

namespace ShippingService.Tests.Assertions;

/// <summary>
/// Tracking codes are case-insensitive in the carrier's API, so equivalency
/// comparisons must not fail on casing differences.
/// </summary>
public class TrackingCodeEquivalencyStep : IEquivalencyStep
{
    public EquivalencyResult Handle(Comparands comparands, IEquivalencyValidationContext context,
        IEquivalencyValidator nestedValidator)
    {
        if (comparands.Subject is string subject
            && comparands.Expectation is string expectation
            && context.CurrentNode.Name == nameof(Shipment.TrackingCode))
        {
            subject.Should().BeEquivalentTo(expectation,
                context.Reason.FormattedMessage, context.Reason.Arguments);

            return EquivalencyResult.AssertionCompleted;
        }

        return EquivalencyResult.ContinueWithNext;
    }
}
