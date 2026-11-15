using System.Reflection;
using VSA.Architecture;

namespace LoadApp.Domain.Shared;

public sealed record AppInput(Architecture Architecture, IReadOnlyList<Assembly> Assemblies, string AppRoot);
