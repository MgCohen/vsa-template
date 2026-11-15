using ArchUnitNET.Domain;
using Steps.Domain.Shared;
using VSA.Architecture;

namespace BuiltApp.Infrastructure;

public sealed record LoadedApp(
    string AppRoot, IReadOnlyList<string> Files, IReadOnlyList<IType> Types,
    IReadOnlyList<TypeFacts> Facts, IReadOnlyList<Finding> Warnings);
