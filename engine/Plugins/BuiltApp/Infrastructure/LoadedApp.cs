namespace BuiltApp.Infrastructure;

using ArchUnitNET.Domain;
using Steps.Domain.Shared;
using Umpire.Architecture;

public sealed record LoadedApp(
    string BlockFolder, IReadOnlyList<string> Files, IReadOnlyList<IType> Types,
    IReadOnlyList<TypeFacts> Facts, IReadOnlyList<Finding> Warnings);
