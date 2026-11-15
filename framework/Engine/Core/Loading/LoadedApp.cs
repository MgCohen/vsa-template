using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed record LoadedApp(
    string AppRoot, IReadOnlyList<string> Files, IReadOnlyList<IType> Types,
    IReadOnlyDictionary<string, Source> Sources, IReadOnlyList<RuleFinding> Warnings);
