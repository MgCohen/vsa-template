using System.Reflection;
using VSA.Architecture;

namespace Definition.Domain.Shared;

public sealed record AppInput(Architecture Architecture, IReadOnlyList<Assembly> Assemblies, string AppRoot);
