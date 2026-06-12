using FluentAssertions.Formatting;

namespace ShippingService.Tests.Assertions;

public class ShipmentFormatter : IValueFormatter
{
    public bool CanHandle(object value) => value is Shipment;

    public void Format(object value, FormattedObjectGraph formattedGraph,
        FormattingContext context, FormatChild formatChild)
    {
        var shipment = (Shipment)value;
        string result = $"Shipment({shipment.Id}, {shipment.Items.Count} items)";

        if (context.UseLineBreaks)
        {
            formattedGraph.AddLine(result);
        }
        else
        {
            formattedGraph.AddFragment(result);
        }
    }
}
