using System.Reflection;
using Umpire.Architecture;

namespace LoadApp.Domain.Shared;

public sealed record AppInput(Architecture Architecture, IReadOnlyList<Assembly> Assemblies, string AppRoot);
