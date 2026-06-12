using FluentAssertions;
using FluentAssertions.Equivalency;
using Xunit;

namespace InventoryApp.Tests;

public class ProductCatalogTests
{
    private static EquivalencyAssertionOptions<Product> CatalogComparison(
        EquivalencyAssertionOptions<Product> options)
    {
        return options
            .RespectingRuntimeTypes()
            .Excluding(p => p.Quantity);
    }

    private static List<Product> SampleCatalog() => new()
    {
        new Product { Sku = "KB-01", Name = "Keyboard", Price = 49.99m, Quantity = 12 },
        new Product { Sku = "MS-02", Name = "Mouse", Price = 19.99m, Quantity = 30 },
        new DiscountedProduct { Sku = "MN-03", Name = "Monitor", Price = 199.99m, Quantity = 5, DiscountPercent = 10m },
    };

    [Fact]
    public void Catalog_contains_at_least_three_products()
    {
        var catalog = SampleCatalog();

        catalog.Should().HaveCountGreaterOrEqualTo(3);
        catalog.Should().HaveCountLessOrEqualTo(100);
    }

    [Fact]
    public void Catalog_products_all_have_skus()
    {
        var catalog = SampleCatalog();

        catalog.Should().OnlyContain(p => p.Sku.Length > 0);
        catalog.Should().AllSatisfy(p => p.Price.Should().BePositive());
    }

    [Fact]
    public void Discounted_product_is_compared_by_its_runtime_type()
    {
        Product actual = new DiscountedProduct
        {
            Sku = "MN-03", Name = "Monitor", Price = 199.99m, Quantity = 99, DiscountPercent = 10m
        };

        Product expected = new DiscountedProduct
        {
            Sku = "MN-03", Name = "Monitor", Price = 199.99m, Quantity = 5, DiscountPercent = 10m
        };

        actual.Should().BeEquivalentTo(expected, CatalogComparison);
    }

    [Fact]
    public void Catalog_comparison_ignores_nested_supplier_details()
    {
        var actual = new Product
        {
            Sku = "KB-01", Name = "Keyboard", Price = 49.99m,
            Supplier = new Supplier { Name = "Acme", Country = "US" }
        };

        var expected = new Product
        {
            Sku = "KB-01", Name = "Keyboard", Price = 49.99m,
            Supplier = new Supplier { Name = "Acme", Country = "DE" }
        };

        actual.Should().BeEquivalentTo(expected, options => options
            .ExcludingNestedObjects()
            .Excluding(p => p.Supplier));
    }

    [Fact]
    public void Sku_format_is_uppercase_letters_dash_digits()
    {
        var product = SampleCatalog()[0];

        product.Sku.Should().MatchRegex("^[A-Z]{2}-[0-9]{2}$");
        product.Name.Should().NotBeNullOrWhiteSpace();
    }
}
