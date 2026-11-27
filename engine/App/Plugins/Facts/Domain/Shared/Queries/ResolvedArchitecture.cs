namespace Umpire.Architecture;

public sealed class ResolvedArchitecture {
    readonly IReadOnlyList<Part> externalParts;
    readonly IReadOnlyDictionary<Part, PartOptions> partOptions;
    readonly IReadOnlyList<CountStatement> counts;
    readonly IReadOnlyList<MatchStatement> matches;
    readonly IReadOnlyList<TraitStatement> traits;
    readonly IReadOnlyList<PlaceStatement> places;

    public ResolvedArchitecture(Architecture architecture) {
        var compiled = new Compiler(architecture);
        Architecture = architecture;
        Compiled = compiled;
        Config = compiled.Config;
        Options = compiled.Options;
        Root = compiled.Root;
        Parts = compiled.Parts;
        UseRules = compiled.UseRules;
        partOptions = compiled.OptionsByPart;
        counts = compiled.Counts;
        matches = compiled.Matches;
        traits = compiled.Traits;
        places = compiled.Places;
        externalParts = [.. Parts.Where(p => p.IsExternal)];
    }

    public Architecture Architecture { get; }
    public ArchitectureConfig Config { get; }
    public PartOptions Options { get; }
    public Part Root { get; }
    public IReadOnlyList<Part> Parts { get; }
    public IReadOnlyList<PartRule> UseRules { get; }
    internal Compiler Compiled { get; }

    public PartOptions OptionsOf(Part part) => partOptions.TryGetValue(part, out var options) ? options : Options;

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
        PartOf.Bind(this, types, blockFolder, innerBlocks, []);

    public Bindings Place(
        IReadOnlyList<TypeFacts> types, string blockFolder, IReadOnlyList<string> innerBlocks,
        IReadOnlyList<FolderAlias> folderAliases) =>
        PartOf.Bind(this, types, blockFolder, innerBlocks, folderAliases);

    public IReadOnlyList<BlockPlace> CoveredBlocks(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        BlockPlaces.Covered(this, blockFolder, innerBlocks);

    public IReadOnlyList<string> BlockPlaceFolders() => BlockPlaces.Patterns(this);

    public IReadOnlyList<BlockPlace> EmptyPlaces(string blockFolder, IReadOnlyList<string> innerBlocks, BlockFolders folders) =>
        BlockPlaces.Empty(this, blockFolder, innerBlocks, folders);

    public Decision DecisionFor(Binding from, Binding to) => DecisionFor(from.Part, from.Values, to.Part, to.Values);

    public Decision DecisionFor(Part from, IReadOnlyDictionary<string, string> fromValues, Part to, IReadOnlyDictionary<string, string> toValues) =>
        new(ActingRule(from, fromValues, to, toValues), OptionsOf(to).DefaultPermission!.Value);

    public PartRule? ActingRule(Part from, IReadOnlyDictionary<string, string> fromValues, Part to, IReadOnlyDictionary<string, string> toValues) =>
        UseRules.LastOrDefault(rule => rule.From == from && rule.To == to && rule.Condition.Holds(fromValues, toValues));

    public IReadOnlyList<CountResult> Counts(Bindings bindings) =>
        [.. counts.SelectMany(statement => CountCheck.Failures(statement, bindings))];

    public IReadOnlyList<MatchResult> Matches(Bindings bindings) =>
        [.. matches.SelectMany(statement => MatchCheck.Failures(statement, bindings))];

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Each answer of the rules is a member of the resolved definition.")]
    public IReadOnlyList<ContentResult> Names(Bindings bindings) => ContentCheck.Names(bindings);

    public IReadOnlyList<ContentResult> Traits(Bindings bindings) => ContentCheck.Traits(traits, bindings);

    public IReadOnlyList<ContentResult> Places(Bindings bindings) => ContentCheck.Places(places, bindings);

    static bool IsUnder(string name, string external) =>
        name == external || name.StartsWith(external + ".", StringComparison.Ordinal);

    static string NamespaceOf(string typeFullName) {
        var outer = typeFullName.Split('+')[0];
        var dot = outer.LastIndexOf('.');
        return dot < 0 ? "" : outer.Substring(0, dot);
    }
}
