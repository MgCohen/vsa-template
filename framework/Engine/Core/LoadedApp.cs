using ArchUnitNET.Domain;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Architecture.Engine;

public sealed record LoadedApp(
    ArchModel Architecture, string AppRoot, IReadOnlyList<string> Files, IReadOnlyList<IType> Types,
    IReadOnlyDictionary<string, Source> Sources, IReadOnlyList<RuleFinding> Warnings);
