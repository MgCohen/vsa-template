namespace BuiltApp.Infrastructure;

using ArchUnitNET.Domain;
using Umpire.Architecture;

public sealed record LoadedApp(
    string BlockFolder, IReadOnlyList<string> Files, IReadOnlyList<IType> Types,
    IReadOnlyList<TypeFacts> Facts, bool MissingScriptAssemblies);
