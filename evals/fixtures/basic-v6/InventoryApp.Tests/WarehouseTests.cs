using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace InventoryApp.Tests;

public class WarehouseTests
{
    [Fact]
    public void Stocking_a_valid_product_adds_it_to_the_warehouse()
    {
        var warehouse = new Warehouse();
        var product = new Product { Sku = "KB-01", Name = "Keyboard", Price = 49.99m, Quantity = 3 };

        warehouse.Stock(product);

        using (new AssertionScope())
        {
            warehouse.Products.Should().ContainSingle()
                .Which.Sku.Should().Be("KB-01");
            warehouse.Products[0].Quantity.Should().BeGreaterOrEqualTo(1);
        }
    }

    [Fact]
    public void Stocking_an_empty_product_throws()
    {
        var warehouse = new Warehouse();
        var product = new Product { Sku = "XX-00", Quantity = 0 };

        Action act = () => warehouse.Stock(product);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*non-positive quantity*")
            .And.ParamName.Should().Be("product");
    }

    [Fact]
    public void Pick_time_stays_within_the_promised_window()
    {
        var warehouse = new Warehouse();

        var estimate = warehouse.EstimatePickTime(itemCount: 10);

        estimate.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(20));
        estimate.Should().BeGreaterOrEqualTo(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Warehouse_starts_empty()
    {
        var warehouse = new Warehouse();

        warehouse.Products.Should().BeEmpty();
    }
}
