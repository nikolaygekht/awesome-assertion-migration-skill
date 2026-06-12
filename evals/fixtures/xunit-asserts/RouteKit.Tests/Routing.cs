namespace RouteKit;

public abstract record Segment(string Raw);

public sealed record LiteralSegment(string Raw) : Segment(Raw);

public sealed record ParameterSegment(string Raw, string Name) : Segment(Raw);

public static class TemplateParser
{
    public static IReadOnlyList<Segment> Parse(string template)
    {
        if (string.IsNullOrEmpty(template) || template[0] != '/')
            throw new ArgumentException("Route templates must start with '/'.", nameof(template));

        return template
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.StartsWith('{') && part.EndsWith('}')
                ? (Segment)new ParameterSegment(part, part[1..^1])
                : new LiteralSegment(part))
            .ToList();
    }

    public static string Normalize(string path) =>
        "/" + string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

    public static double ParameterDensity(string template)
    {
        var segments = Parse(template);
        return segments.Count == 0
            ? 0
            : (double)segments.Count(s => s is ParameterSegment) / segments.Count;
    }
}

public sealed class RouteTable
{
    private readonly List<string> _templates = new();

    public IReadOnlyList<string> Templates => _templates;

    public void Add(string template)
    {
        TemplateParser.Parse(template);
        _templates.Add(template);
    }

    public string? Match(string path)
    {
        var parts = TemplateParser.Normalize(path)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var template in _templates)
        {
            var segments = TemplateParser.Parse(template);
            if (segments.Count != parts.Length)
                continue;

            var matches = segments
                .Zip(parts, (segment, part) => segment is ParameterSegment
                    || string.Equals(segment.Raw, part, StringComparison.OrdinalIgnoreCase))
                .All(m => m);
            if (matches)
                return template;
        }

        return null;
    }

    public async Task<string> MatchAsync(string path)
    {
        await Task.Yield();
        return Match(path) ?? throw new InvalidOperationException($"No route matches '{path}'.");
    }
}
