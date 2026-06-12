using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace ShippingService.Tests.Assertions;

public static class StringAssertionsExtensions
{
    [CustomAssertion]
    public static AndConstraint<StringAssertions> BeTrackingCode(
        this StringAssertions assertions, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(assertions.Subject is not null
                && Regex.IsMatch(assertions.Subject, "^TRK-[A-Z0-9]{8}$"))
            .FailWith("Expected {context:string} to be a tracking code (TRK-XXXXXXXX){reason}, but found {0}.",
                assertions.Subject);

        return new AndConstraint<StringAssertions>(assertions);
    }
}
