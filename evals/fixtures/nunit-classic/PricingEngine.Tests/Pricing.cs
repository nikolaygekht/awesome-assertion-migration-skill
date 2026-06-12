namespace PricingEngine;

public record LineItem(string Sku, string Category, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public enum CustomerTier { Standard, Silver, Gold }

public class Quote
{
    public required string InvoiceNumber { get; init; }
    public required IReadOnlyList<LineItem> Items { get; init; }
    public decimal Subtotal { get; init; }
    public double DiscountRate { get; init; }
    public decimal Total { get; init; }
    public string? Notes { get; init; }
}

public class PriceCalculator
{
    public static readonly IReadOnlyList<string> KnownCategories =
        new[] { "hardware", "software", "services" };

    public decimal Subtotal(IEnumerable<LineItem> items) => items.Sum(i => i.Total);

    public double DiscountRate(CustomerTier tier, decimal subtotal)
    {
        var rate = tier switch
        {
            CustomerTier.Gold => 0.15,
            CustomerTier.Silver => 0.10,
            _ => 0.0,
        };
        if (subtotal >= 10_000m)
            rate += 0.05;
        return rate;
    }

    public Quote BuildQuote(int year, int sequence, CustomerTier tier, IReadOnlyList<LineItem> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("A quote needs at least one line item.", nameof(items));

        var subtotal = Subtotal(items);
        var rate = DiscountRate(tier, subtotal);
        return new Quote
        {
            InvoiceNumber = $"INV-{year:D4}-{sequence:D5}",
            Items = items,
            Subtotal = subtotal,
            DiscountRate = rate,
            Total = subtotal * (1 - (decimal)rate),
            Notes = tier == CustomerTier.Gold ? "Priority Handling" : null,
        };
    }

    public static string ParseSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku) || !sku.Contains('-'))
            throw new FormatException($"'{sku}' is not a valid SKU.");
        return sku.Split('-')[0].ToUpperInvariant();
    }

    public async Task<Quote> QuoteAsync(int year, int sequence, CustomerTier tier, IReadOnlyList<LineItem> items)
    {
        await Task.Yield();
        if (items.Count == 0)
            throw new InvalidOperationException("Cannot quote an empty basket.");
        return BuildQuote(year, sequence, tier, items);
    }

    public IReadOnlyList<string> CategoriesIn(IEnumerable<LineItem> items) =>
        items.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();
}
