namespace ArchitectureViewer.Presenters.Items;

public sealed record GraphKind(string Name, string Shape)
{
    public static readonly GraphKind App = new("App", "Fields");
    public static readonly GraphKind Definition = new("Definition", "Part");

    public static readonly IReadOnlyList<GraphKind> All = [App, Definition];
}
