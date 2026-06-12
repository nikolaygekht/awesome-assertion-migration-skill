namespace ShippingService.Tests;

public enum ShipmentStatus
{
    Pending,
    InTransit,
    Delivered,
}

public class ShipmentItem
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
}

public class Shipment
{
    public string Id { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public ShipmentStatus Status { get; set; }
    public List<ShipmentItem> Items { get; set; } = new();
}
