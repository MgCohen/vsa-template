namespace Umpire.Architecture;

public abstract class ArchitectureTest<TArchitecture> where TArchitecture : Architecture, new() {
    static readonly string BlockFolder =
        System.IO.Path.GetFullPath(System.IO.Path.DirectorySeparatorChar + "umpire-block");

    ResolvedArchitecture? resolved;

    protected TArchitecture Definition => (TArchitecture)Resolved.Architecture;

    ResolvedArchitecture Resolved => resolved ??= new ResolvedArchitecture(new TArchitecture());

    protected Place At<T>(string folder) => At(typeof(T), folder);

    protected Place At(Type type, string folder) => Bind(folder, TypeFactsOf.Type(type, SourceIn(folder, type.Name)));

    protected Place At(string folder) => Bind(folder, TypeFactsOf.Plain("PlainType", SourceIn(folder, "PlainType")));

    protected Decision MayUse(Place from, Place to) {
        var user = from.Binding ?? throw NotMapped(from);
        var target = to.Binding ?? throw NotMapped(to);
        return Resolved.DecisionFor(user, target);
    }

    static Source SourceIn(string folder, string name) =>
        new(System.IO.Path.Combine(BlockFolder, string.Join("/", Segments(folder)), name + ".cs"), 1);

    static string[] Segments(string folder) =>
        folder.Replace('\\', '/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);

    static bool Covers(string[] pattern, string[] segments) =>
        pattern.Length <= segments.Length
        && pattern.Select((segment, i) => Fits(segment, segments[i])).All(fits => fits);

    static bool Fits(string pattern, string folder) =>
        pattern.StartsWith("{", StringComparison.Ordinal)
        || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);

    static InvalidOperationException NotMapped(Place place) =>
        new($"No part takes a type in {place.Folder}. Use a folder of the definition, or assert IsMapped.");

    Place Bind(string folder, TypeFacts type) {
        var bindings = Resolved.Place([type], BlockFolder, InnerBlocks(folder));
        return new Place(folder, bindings.All.SingleOrDefault());
    }

    IReadOnlyList<string> InnerBlocks(string folder) {
        var segments = Segments(folder);
        return [.. Definition.Parts()
            .Where(p => p.IsBlockPlace)
            .Select(p => p.Path.Split('/'))
            .Where(pattern => Covers(pattern, segments))
            .Select(pattern => System.IO.Path.Combine(BlockFolder, string.Join("/", segments.Take(pattern.Length))))];
    }
}
