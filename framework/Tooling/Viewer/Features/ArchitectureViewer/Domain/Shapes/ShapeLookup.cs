namespace ArchitectureViewer.Domain.Shapes;

public static class ShapeLookup
{
    public static INodeShape Named(this IEnumerable<INodeShape> shapes, string name)
    {
        return shapes.First(s => s.Name == name);
    }

    public static IReadOnlyList<string> Names(this IEnumerable<INodeShape> shapes)
    {
        return [.. shapes.Select(s => s.Name).Order(StringComparer.Ordinal)];
    }
}
