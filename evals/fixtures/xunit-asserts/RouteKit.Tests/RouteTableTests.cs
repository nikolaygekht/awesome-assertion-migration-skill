using Xunit;

namespace RouteKit.Tests;

public class RouteTableTests
{
    private static RouteTable BuildTable()
    {
        var table = new RouteTable();
        table.Add("/health");
        table.Add("/users/{id}");
        table.Add("/users/{id}/posts");
        return table;
    }

    [Fact]
    public void Match_finds_registered_routes()
    {
        var table = BuildTable();

        Assert.Equal("/users/{id}", table.Match("/Users/42"));
        Assert.NotNull(table.Match("/health"));
        Assert.Contains("/health", table.Templates);
        Assert.DoesNotContain("/admin", table.Templates);
        Assert.NotEmpty(table.Templates);
        Assert.InRange(table.Templates.Count, 1, 10);
        Assert.Same(table.Templates, table.Templates);
    }

    [Fact]
    public void Match_returns_null_for_unknown_paths()
    {
        var table = BuildTable();

        Assert.Null(table.Match("/nope"));
        Assert.True(table.Match("/health") != null, "the health route must stay registered");
        Assert.False(table.Match("/users/42/comments") != null);
        Assert.Empty(new RouteTable().Templates);
    }

    [Fact]
    public async Task MatchAsync_throws_for_unknown_paths()
    {
        var table = BuildTable();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => table.MatchAsync("/missing"));
        Assert.Contains("/missing", ex.Message);

        var matched = await table.MatchAsync("/users/7");
        Assert.Equal("/users/{id}", matched);
    }
}
