namespace InventoryApp.Tests;

public class Product
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public Supplier? Supplier { get; set; }
}

public class Supplier
{
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
}

public class DiscountedProduct : Product
{
    public decimal DiscountPercent { get; set; }
}

public class Warehouse
{
    private readonly List<Product> products = new();

    public IReadOnlyList<Product> Products => products;

    public void Stock(Product product)
    {
        if (product.Quantity <= 0)
        {
            throw new ArgumentException("Cannot stock a product with non-positive quantity", nameof(product));
        }

        products.Add(product);
    }

    public TimeSpan EstimatePickTime(int itemCount) => TimeSpan.FromSeconds(itemCount * 1.5);
}
