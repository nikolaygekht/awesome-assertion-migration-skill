using NUnit.Framework;

namespace PricingEngine.Tests;

[TestFixture]
public class ConstraintModelTests
{
    private static readonly IReadOnlyList<LineItem> Basket = new[]
    {
        new LineItem("HW-100", "hardware", 2, 250m),
        new LineItem("SW-200", "software", 1, 1500m),
    };

    private readonly PriceCalculator _calc = new();

    [Test]
    public void Subtotal_constraints()
    {
        var subtotal = _calc.Subtotal(Basket);

        Assert.That(subtotal, Is.EqualTo(2000m));
        Assert.That(subtotal, Is.Not.EqualTo(0m));
        Assert.That(subtotal, Is.GreaterThan(1000m));
        Assert.That(subtotal, Is.InRange(1500m, 2500m));
        Assert.That(_calc.DiscountRate(CustomerTier.Standard, 100m), Is.Zero);
    }

    [Test]
    public void Discount_rates_with_tolerance()
    {
        var gold = _calc.DiscountRate(CustomerTier.Gold, 2000m);
        var goldBulk = _calc.DiscountRate(CustomerTier.Gold, 12_000m);

        Assert.That(gold, Is.EqualTo(0.15).Within(0.0001));
        Assert.That(goldBulk, Is.EqualTo(0.20).Within(1).Percent);
        Assert.That(gold, Is.Positive);
        Assert.That(goldBulk, Is.LessThanOrEqualTo(0.25));
    }

    [Test]
    public void Invoice_number_constraints()
    {
        var quote = _calc.BuildQuote(2026, 42, CustomerTier.Gold, Basket);
        var standard = _calc.BuildQuote(2026, 43, CustomerTier.Standard, Basket);

        Assert.That(quote.InvoiceNumber, Does.StartWith("INV-"));
        Assert.That(quote.InvoiceNumber, Does.Contain("00042"));
        Assert.That(quote.InvoiceNumber, Does.Match(@"\d{5}$"));
        Assert.That(quote.InvoiceNumber, Is.EqualTo("inv-2026-00042").IgnoreCase);
        Assert.That(quote.InvoiceNumber, Is.Not.Empty);
        Assert.That(quote.Notes, Is.Not.Null);
        Assert.That(standard.Notes, Is.Null);
        Assert.That(quote.InvoiceNumber, Does.Not.Contain(" "));
    }

    [Test]
    public void Category_collection_constraints()
    {
        var categories = _calc.CategoriesIn(Basket);

        Assert.That(categories.Count == 2);
        Assert.That(categories, Has.Count.EqualTo(2));
        Assert.That(categories, Has.Member("hardware"));
        Assert.That(categories, Is.EquivalentTo(new[] { "software", "hardware" }));
        Assert.That(categories, Is.Ordered);
        Assert.That(categories, Is.Unique);
        Assert.That(categories, Is.SubsetOf(PriceCalculator.KnownCategories));
        Assert.That(categories, Has.Exactly(1).Matches<string>(c => c.StartsWith("soft")));
        Assert.That(categories, Has.Some.EqualTo("software"));
        Assert.That(categories, Has.All.Matches<string>(c => c.Length > 5));
    }

    [Test]
    public void Exception_constraints()
    {
        Assert.That(() => PriceCalculator.ParseSku("nope"),
            Throws.TypeOf<FormatException>().With.Message.Contains("nope"));
        Assert.That(() => _calc.BuildQuote(2026, 1, CustomerTier.Standard, Array.Empty<LineItem>()),
            Throws.ArgumentException);
        Assert.That(() => PriceCalculator.ParseSku(""), Throws.InstanceOf<SystemException>());
        Assert.That(() => PriceCalculator.ParseSku("HW-1"), Throws.Nothing);
    }

    [Test]
    public void Async_quotes_report_empty_baskets()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _calc.QuoteAsync(2026, 9, CustomerTier.Gold, Array.Empty<LineItem>()));
        Assert.That(ex!.Message, Does.Contain("empty basket"));

        Assert.DoesNotThrowAsync(() => _calc.QuoteAsync(2026, 10, CustomerTier.Gold, Basket));

        var quote = _calc.BuildQuote(2026, 11, CustomerTier.Gold, Basket);
        Assert.That(quote.Total, Is.EqualTo(1700m), "gold discount should be applied");
    }
}
