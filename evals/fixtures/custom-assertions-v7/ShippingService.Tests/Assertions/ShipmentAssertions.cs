using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace ShippingService.Tests.Assertions;

public static class ShipmentExtensions
{
    public static ShipmentAssertions Should(this Shipment instance)
    {
        return new ShipmentAssertions(instance);
    }
}

public class ShipmentAssertions
    : ReferenceTypeAssertions<Shipment, ShipmentAssertions>
{
    public ShipmentAssertions(Shipment instance)
        : base(instance)
    {
    }

    protected override string Identifier => "shipment";

    [CustomAssertion]
    public AndConstraint<ShipmentAssertions> BeDelivered(
        string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .WithExpectation("Expected {context:shipment} to be delivered{reason}, ")
            .ForCondition(Subject is not null)
            .FailWith("but found <null>.")
            .Then
            .ForCondition(Subject!.Status == ShipmentStatus.Delivered)
            .FailWith("but it is {0}.", Subject.Status)
            .Then
            .ClearExpectation();

        return new AndConstraint<ShipmentAssertions>(this);
    }

    [CustomAssertion]
    public AndWhichConstraint<ShipmentAssertions, ShipmentItem> ContainItem(
        string sku, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(sku))
            .FailWith("You can't assert that the shipment contains an item without a SKU.")
            .Then
            .Given(() => Subject.Items)
            .ForCondition(items => items.Any(item => item.Sku == sku))
            .FailWith("Expected {context:shipment} to contain an item with SKU {0}{reason}, but found {1}.",
                _ => sku, items => items.Select(item => item.Sku));

        ShipmentItem? match = Subject.Items.FirstOrDefault(item => item.Sku == sku);

        return new AndWhichConstraint<ShipmentAssertions, ShipmentItem>(this, match!);
    }

    [CustomAssertion]
    public AndConstraint<ShipmentAssertions> HaveItemsInAllBoxes(
        string because = "", params object[] becauseArgs)
    {
        foreach (ShipmentItem item in Subject.Items)
        {
            using (new AssertionScope(item.Sku))
            {
                item.Quantity.Should().BePositive(because, becauseArgs);
            }
        }

        return new AndConstraint<ShipmentAssertions>(this);
    }
}
