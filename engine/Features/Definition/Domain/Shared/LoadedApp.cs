using System.Reflection;
using VSA.Architecture;

namespace Definition.Domain.Shared;

public sealed record LoadedApp(ArchitectureDefinition Definition, IReadOnlyList<Assembly> Assemblies, string AppRoot);
