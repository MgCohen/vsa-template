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

    public Bindings Place(IReadOnlyList<TypeFacts> types, string appRoot) => Match.Bind(this, types, appRoot);

    public Decision DecisionFor(Binding from, Binding to) =>
        Decide.For(Architecture, from.Part, from.Values, to.Part, to.Values);
}
