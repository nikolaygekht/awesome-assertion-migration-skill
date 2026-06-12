using Xunit;

namespace RouteKit.Tests;

public class TemplateParserTests
{
    [Fact]
    public void Parse_splits_literals_and_parameters()
    {
        var segments = TemplateParser.Parse("/users/{id}/posts");

        Assert.Equal(3, segments.Count);
        Assert.Equal(new[] { "users", "{id}", "posts" }, segments.Select(s => s.Raw).ToArray());
        Assert.NotEqual(0, segments.Count);
    }

    [Fact]
    public void Parameter_segments_carry_their_name()
    {
        var segment = TemplateParser.Parse("/users/{id}")[1];

        var parameter = Assert.IsType<ParameterSegment>(segment);
        Assert.Equal("id", parameter.Name);
        Assert.IsAssignableFrom<Segment>(segment);
        Assert.IsNotType<LiteralSegment>(segment);
        Assert.Equivalent(new { Name = "id" }, parameter);
    }

    [Fact]
    public void Parse_rejects_templates_without_leading_slash()
    {
        var ex = Assert.Throws<ArgumentException>(() => TemplateParser.Parse("users"));
        Assert.Equal("template", ex.ParamName);

        Assert.ThrowsAny<SystemException>(() => TemplateParser.Parse(""));
    }

    [Fact]
    public void Normalize_lowercases_and_collapses_slashes()
    {
        var normalized = TemplateParser.Normalize("API/Users/");

        Assert.Equal("/api/users", normalized);
        Assert.StartsWith("/", normalized);
        Assert.EndsWith("users", normalized);
        Assert.DoesNotContain("//", normalized);
        Assert.Matches("^/[a-z/]+$", normalized);
    }

    [Theory]
    [InlineData("/health", 1)]
    [InlineData("/users/{id}", 2)]
    [InlineData("/users/{id}/posts/{postId}", 4)]
    public void Parse_counts_segments(string template, int expected)
    {
        Assert.Equal(expected, TemplateParser.Parse(template).Count);
    }

    [Fact]
    public void Single_parameter_template_has_one_segment()
    {
        var only = Assert.Single(TemplateParser.Parse("/{tenant}"));

        Assert.IsType<ParameterSegment>(only);
    }

    [Fact]
    public void Segments_hold_their_shape()
    {
        var segments = TemplateParser.Parse("/users/{id}/posts");

        Assert.All(segments, s => Assert.False(string.IsNullOrEmpty(s.Raw)));
        Assert.Collection(segments,
            s => Assert.Equal("users", s.Raw),
            s => Assert.IsType<ParameterSegment>(s),
            s => Assert.Equal("posts", s.Raw));
        Assert.Distinct(segments.Select(s => s.Raw));
        Assert.Contains(segments, s => s is ParameterSegment);
    }

    [Fact]
    public void Parameter_density_is_a_fraction_of_segments()
    {
        var density = TemplateParser.ParameterDensity("/users/{id}/posts");

        Assert.Equal(0.333, density, 3);
        Assert.InRange(density, 0.0, 1.0);
    }
}
