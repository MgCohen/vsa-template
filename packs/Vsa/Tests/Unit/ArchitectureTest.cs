namespace Umpire.Architecture;

public abstract class ArchitectureTest<TArchitecture> where TArchitecture : Architecture, new() {
    static readonly string BlockFolder =
        System.IO.Path.GetFullPath(System.IO.Path.DirectorySeparatorChar + "umpire-block");

    ResolvedArchitecture? resolved;

    protected TArchitecture Definition => (TArchitecture)Resolved.Architecture;

    ResolvedArchitecture Resolved => resolved ??= new ResolvedArchitecture(new TArchitecture());

    protected Place At<T>(string folder, params Type[] neighbors) => At(typeof(T), folder, neighbors);

    protected Place At(Type type, string folder, params Type[] neighbors) {
        var facts = new[] { type }.Concat(neighbors).Distinct()
            .Select(t => TypeFactsOf.Type(t, SourceIn(folder, t.Name)))
            .ToList();
        var bindings = Resolved.Place(facts, BlockFolder, InnerBlocks(folder));
        return new Place(folder, bindings.Of(facts[0].FullName)) { Placed = bindings };
    }

    protected Place At(string folder) {
        var facts = TypeFactsOf.Plain("PlainType", SourceIn(folder, "PlainType"));
        return new Place(folder, Resolved.Place([facts], BlockFolder, InnerBlocks(folder)).All.SingleOrDefault());
    }

    protected Decision MayUse(Place from, Place to) {
        var user = from.Binding ?? throw NotMapped(from);
        var target = to.Binding ?? throw NotMapped(to);
        return Resolved.DecisionFor(user, target);
    }

    protected IReadOnlyList<string> Problems(Place place) {
        var binding = place.Binding ?? throw NotMapped(place);
        var bindings = place.Placed!;
        return [.. Resolved.Names(bindings).Concat(Resolved.Traits(bindings)).Concat(Resolved.Places(bindings))
            .Where(result => result.At == binding)
            .Select(result => result.Message)];
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

    IReadOnlyList<string> InnerBlocks(string folder) {
        var segments = Segments(folder);
        return [.. Resolved.Parts
            .Where(p => p.IsBlockPlace && !p.IsExternal && (p.HasFolder || p.IsGroup))
            .Select(p => p.Path.Split('/'))
            .Where(pattern => Covers(pattern, segments))
            .Select(pattern => System.IO.Path.Combine(BlockFolder, string.Join("/", segments.Take(pattern.Length))))];
    }
}
