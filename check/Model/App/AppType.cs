namespace Umpire.CheckModel;

public sealed class AppType {
    readonly IReadOnlyList<TypeFacts> entries;
    readonly Lazy<IReadOnlyList<AppReference>> references;

    public AppType(string project, IReadOnlyList<TypeFile> files, Func<IReadOnlyList<AppReference>>? current = null) {
        Project = project;
        Files = [.. files.OrderBy(f => f.IsGenerated).ThenBy(f => f.Facts.Location.Line).ThenBy(f => f.File, StringComparer.Ordinal)];
        entries = [.. Files.Select(f => f.Facts)];
        Name = entries[0].Name;
        Content = Merged(entries);
        references = new(current ?? (() => [.. Files.SelectMany(f => f.Facts.References.Select(r => new AppReference(f.File, r)))]));
    }

    public TypeName Name { get; }
    public string Project { get; }
    public IReadOnlyList<TypeFile> Files { get; }
    public TypeContent Content { get; }
    public IReadOnlyList<AppReference> References => references.Value;
    public string File => Files[0].File;
    public int Line => Files[0].Facts.Location.Line;
    public int Column => Files[0].Facts.Location.Column;
    public bool IsNested => entries[0].Location.DeclaringType is not null;

    public Placement? Placement { get; internal set; }
    public string? PlaceProblem { get; internal set; }
    public bool IsUnmapped { get; internal set; }

    public bool Is(TypeName type) => Content.Bases.Any(b => b.FullName == type.FullName);

    public bool Mentions(string fullName) => References.Any(r => r.Fact.Target.FullName == fullName);

    static TypeContent Merged(IReadOnlyList<TypeFacts> entries) => entries.Count == 1
        ? entries[0].Content
        : entries[0].Content with {
            Attributes = [.. entries.SelectMany(e => e.Content.Attributes)],
            Members = [.. entries.SelectMany(e => e.Content.Members)],
        };
}
