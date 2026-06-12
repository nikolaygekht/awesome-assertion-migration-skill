using NUnit.Framework;

namespace PricingEngine.Tests;

[TestFixture]
public class ClassicAssertTests
{
    private static readonly IReadOnlyList<LineItem> Basket = new[]
    {
        new LineItem("HW-100", "hardware", 2, 250m),
        new LineItem("SW-200", "software", 1, 1500m),
    };

    private readonly PriceCalculator _calc = new();

    [Test]
    public void Subtotal_adds_all_line_totals()
    {
        var subtotal = _calc.Subtotal(Basket);

        Assert.AreEqual(2000m, subtotal);
        Assert.AreNotEqual(0m, subtotal);
        Assert.Positive(subtotal);
    }

    [Test]
    public void Gold_discount_rate_is_fifteen_percent()
    {
        var rate = _calc.DiscountRate(CustomerTier.Gold, 2000m);

        Assert.AreEqual(0.15, rate, 0.0001);
        Assert.Greater(rate, 0.0);
        Assert.LessOrEqual(rate, 0.20);
        Assert.GreaterOrEqual(rate, _calc.DiscountRate(CustomerTier.Silver, 2000m));
        Assert.Zero(_calc.DiscountRate(CustomerTier.Standard, 100m));
    }

    [Test]
    public void Quote_applies_discount_and_keeps_items()
    {
        var quote = _calc.BuildQuote(2026, 42, CustomerTier.Gold, Basket);

        Assert.IsTrue(quote.Total < quote.Subtotal, "discount must reduce the total");
        Assert.IsFalse(quote.Total == 0m);
        Assert.AreSame(Basket, quote.Items);
        Assert.IsNotNull(quote.Notes);
        Assert.IsNull(_calc.BuildQuote(2026, 43, CustomerTier.Standard, Basket).Notes);
        Assert.Less(quote.Total, 2000m);
    }

    [Test]
    public void Invoice_number_is_formatted()
    {
        var quote = _calc.BuildQuote(2026, 42, CustomerTier.Gold, Basket);

        StringAssert.StartsWith("INV-", quote.InvoiceNumber);
        StringAssert.Contains("2026", quote.InvoiceNumber);
        StringAssert.IsMatch(@"^INV-\d{4}-\d{5}$", quote.InvoiceNumber);
        StringAssert.AreEqualIgnoringCase("inv-2026-00042", quote.InvoiceNumber);
        Assert.IsNotEmpty(quote.InvoiceNumber);
    }

    [Test]
    public void Categories_are_distinct_and_sorted()
    {
        var categories = _calc.CategoriesIn(Basket);

        Assert.AreEqual(new[] { "hardware", "software" }, categories);
        CollectionAssert.AreEqual(new[] { "hardware", "software" }, categories);
        CollectionAssert.AreEquivalent(new[] { "software", "hardware" }, categories);
        CollectionAssert.Contains(categories, "software");
        CollectionAssert.DoesNotContain(categories, "services");
        CollectionAssert.AllItemsAreUnique(categories);
        CollectionAssert.AllItemsAreNotNull(categories);
        CollectionAssert.IsNotEmpty(categories);
        CollectionAssert.IsSubsetOf(categories, PriceCalculator.KnownCategories);
        CollectionAssert.IsOrdered(categories);
    }

    [Test]
    public void Sku_parsing_validates_format()
    {
        Assert.AreEqual("HW", PriceCalculator.ParseSku("hw-100"));
        Assert.DoesNotThrow(() => PriceCalculator.ParseSku("SW-1"));

        var ex = Assert.Throws<FormatException>(() => PriceCalculator.ParseSku("bogus"));
        Assert.IsNotNull(ex);
        StringAssert.Contains("bogus", ex!.Message);
        Assert.IsInstanceOf<SystemException>(ex);
        Assert.IsNotInstanceOf<ArgumentException>(ex);

        Assert.Catch<Exception>(() => PriceCalculator.ParseSku(""));
    }

    [Test]
    public void Empty_basket_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => _calc.BuildQuote(2026, 1, CustomerTier.Standard, Array.Empty<LineItem>()));

        Assert.AreEqual("items", ex!.ParamName);
    }

    [Test]
    public void Quote_summary_holds_together()
    {
        var quote = _calc.BuildQuote(2026, 7, CustomerTier.Silver, Basket);

        Assert.Multiple(() =>
        {
            Assert.AreEqual(2000m, quote.Subtotal);
            Assert.AreEqual(0.10, quote.DiscountRate, 1e-9);
            Assert.AreEqual(1800m, quote.Total);
            Assert.IsNull(quote.Notes);
        });
    }
}
