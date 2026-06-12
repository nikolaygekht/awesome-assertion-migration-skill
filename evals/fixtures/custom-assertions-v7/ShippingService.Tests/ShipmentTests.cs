using FluentAssertions;
using ShippingService.Tests.Assertions;
using Xunit;
using Xunit.Sdk;

namespace ShippingService.Tests;

public class ShipmentTests
{
    private static Shipment DeliveredShipment() => new()
    {
        Id = "SH-001",
        TrackingCode = "TRK-A1B2C3D4",
        Status = ShipmentStatus.Delivered,
        Items = new List<ShipmentItem>
        {
            new() { Sku = "KB-01", Quantity = 2 },
            new() { Sku = "MS-02", Quantity = 1 },
        },
    };

    [Fact]
    public void Delivered_shipment_passes_the_custom_assertion()
    {
        var shipment = DeliveredShipment();

        shipment.Should().BeDelivered();
    }

    [Fact]
    public void Pending_shipment_fails_with_a_helpful_message()
    {
        var shipment = DeliveredShipment();
        shipment.Status = ShipmentStatus.Pending;

        Action act = () => shipment.Should().BeDelivered("the courier confirmed it");

        act.Should().Throw<XunitException>()
            .WithMessage("Expected shipment to be delivered because the courier confirmed it, but it is *Pending*");
    }

    [Fact]
    public void ContainItem_supports_chaining_into_the_matched_item()
    {
        var shipment = DeliveredShipment();

        shipment.Should().ContainItem("KB-01")
            .Which.Quantity.Should().Be(2);
    }

    [Fact]
    public void ContainItem_reports_the_available_skus_when_missing()
    {
        var shipment = DeliveredShipment();

        Action act = () => shipment.Should().ContainItem("XX-99");

        act.Should().Throw<XunitException>()
            .WithMessage("*to contain an item with SKU \"XX-99\"*");
    }

    [Fact]
    public void All_boxes_have_positive_quantities()
    {
        var shipment = DeliveredShipment();

        shipment.Should().HaveItemsInAllBoxes();
    }

    [Fact]
    public void Tracking_codes_are_validated_by_the_string_extension()
    {
        var shipment = DeliveredShipment();

        shipment.TrackingCode.Should().BeTrackingCode();
    }

    [Fact]
    public void Invalid_tracking_code_fails_the_string_extension()
    {
        Action act = () => "not-a-code".Should().BeTrackingCode();

        act.Should().Throw<XunitException>()
            .WithMessage("*to be a tracking code*");
    }

    [Fact]
    public void Custom_formatter_renders_shipments_in_failure_messages()
    {
        var shipment = DeliveredShipment();

        Action act = () => shipment.Should().BeNull();

        act.Should().Throw<XunitException>()
            .WithMessage("*Shipment(SH-001, 2 items)*");
    }

    [Fact]
    public void Tracking_code_comparison_ignores_casing_via_the_equivalency_step()
    {
        var actual = DeliveredShipment();
        var expected = DeliveredShipment();
        expected.TrackingCode = "trk-a1b2c3d4";

        // cast: the custom Should(this Shipment) extension hides the object-level one
        ((object)actual).Should().BeEquivalentTo(expected);
    }
}
