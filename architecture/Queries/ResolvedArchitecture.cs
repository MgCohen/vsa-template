namespace Umpire.Architecture;

public sealed class ResolvedArchitecture {
    readonly IReadOnlyList<Part> externalParts;
    readonly IReadOnlyDictionary<Part, Options> partOptions;
    readonly IReadOnlyList<CountStatement> counts;
    readonly IReadOnlyList<MatchStatement> matches;
    readonly IReadOnlyList<KindStatement> kinds;
    readonly IReadOnlyList<OwnerStatement> owners;

    public ResolvedArchitecture(Architecture architecture) {
        var compiled = new Compiler(architecture);
        Architecture = architecture;
        Options = compiled.Options;
        Block = compiled.Block;
        Parts = compiled.Parts;
        UseRules = compiled.UseRules;
        partOptions = compiled.PartOptions;
        counts = compiled.Counts;
        matches = compiled.Matches;
        kinds = compiled.Kinds;
        owners = compiled.Owners;
        externalParts = [.. Parts.Where(p => p.IsExternal)];
    }

    public Architecture Architecture { get; }
    public Options Options { get; }
    public Block Block { get; }
    public IReadOnlyList<Part> Parts { get; }
    public IReadOnlyList<PartRule> UseRules { get; }

    public Options OptionsOf(Part part) => partOptions.TryGetValue(part, out var options) ? options : Options;

    public Part? ExternalPart(string assembly) => ExternalPart(assembly, "");

    public Part? ExternalPart(string assembly, string externalTypeFullName) {
        var space = NamespaceOf(externalTypeFullName);
        return externalParts
            .Where(p => IsUnder(assembly, p.Assembly!) || IsUnder(space, p.Assembly!))
            .OrderByDescending(p => p.Assembly!.Length)
            .FirstOrDefault();
    }

    public Bindings Place(IReadOnlyList<TypeFacts> types, string blockFolder) => Place(types, blockFolder, []);

    public Bindings Place(IReadOnlyList<TypeFacts> types, string blockFolder, IReadOnlyList<string> innerBlocks) =>
        PartOf.Bind(this, types, blockFolder, innerBlocks);

    public IReadOnlyList<BlockPlace> CoveredBlocks(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        BlockPlaces.Covered(this, blockFolder, innerBlocks);

    public IReadOnlyList<BlockPlace> EmptyPlaces(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        BlockPlaces.Empty(this, blockFolder, innerBlocks);

    public Decision DecisionFor(Binding from, Binding to) => DecisionFor(from.Part, from.Values, to.Part, to.Values);

    public Decision DecisionFor(Part from, IReadOnlyDictionary<string, string> fromValues, Part to, IReadOnlyDictionary<string, string> toValues) =>
        new(UseRules.LastOrDefault(rule => rule.From == from && rule.To == to && rule.Condition.Holds(fromValues, toValues)),
            OptionsOf(to).DefaultPermission!.Value);

    public IReadOnlyList<CountResult> Counts(Bindings bindings) =>
        [.. counts.SelectMany(statement => CountCheck.Failures(statement, bindings))];

    public IReadOnlyList<MatchResult> Matches(Bindings bindings) =>
        [.. matches.SelectMany(statement => MatchCheck.Failures(statement, bindings))];

    public IReadOnlyList<ContentResult> Names(Bindings bindings) => ContentCheck.Names(bindings);

    public IReadOnlyList<ContentResult> Kinds(Bindings bindings) => ContentCheck.Kinds(kinds, bindings);

    public IReadOnlyList<ContentResult> Owners(Bindings bindings) => ContentCheck.Owners(owners, bindings);

    static bool IsUnder(string name, string external) =>
        name == external || name.StartsWith(external + ".", StringComparison.Ordinal);

    static string NamespaceOf(string typeFullName) {
        var outer = typeFullName.Split('+')[0];
        var dot = outer.LastIndexOf('.');
        return dot < 0 ? "" : outer[..dot];
    }
}
