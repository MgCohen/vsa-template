namespace Umpire.Architecture;

public sealed class ResolvedArchitecture {
    readonly IReadOnlyList<Part> externalParts;

    public ResolvedArchitecture(Architecture architecture) {
        Architecture = architecture;
        externalParts = [.. architecture.Parts().Where(p => p.IsExternal)];
    }

    public Architecture Architecture { get; }

    public Part? ExternalPart(string assembly) => externalParts
        .Where(p => assembly == p.Assembly || assembly.StartsWith(p.Assembly + ".", StringComparison.Ordinal))
        .OrderByDescending(p => p.Assembly!.Length)
        .FirstOrDefault();

    public Bindings Place(IReadOnlyList<TypeFacts> types, string blockFolder) => Place(types, blockFolder, []);

    public Bindings Place(IReadOnlyList<TypeFacts> types, string blockFolder, IReadOnlyList<string> innerBlocks) =>
        PartOf.Bind(this, types, blockFolder, innerBlocks);

    public IReadOnlyList<BlockPlace> CoveredBlocks(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        BlockPlaces.Covered(Architecture, blockFolder, innerBlocks);

    public IReadOnlyList<BlockPlace> EmptyPlaces(string blockFolder, IReadOnlyList<string> innerBlocks) =>
        BlockPlaces.Empty(Architecture, blockFolder, innerBlocks);

    public Decision DecisionFor(Binding from, Binding to) =>
        MayUse.For(Architecture, from.Part, from.Values, to.Part, to.Values);

    public IReadOnlyList<CountResult> Counts(Bindings bindings) =>
        [.. Architecture.CountStatements().SelectMany(statement => CountCheck.Failures(statement, bindings))];

    public IReadOnlyList<MatchResult> Matches(Bindings bindings) =>
        [.. Architecture.MatchStatements().SelectMany(statement => MatchCheck.Failures(statement, bindings))];
}
