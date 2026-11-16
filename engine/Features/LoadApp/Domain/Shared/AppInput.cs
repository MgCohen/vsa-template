namespace LoadApp.Domain.Shared;

using System.Reflection;
using Umpire.Architecture;

public sealed record AppInput(Architecture Architecture, IReadOnlyList<Assembly> Assemblies, string BlockFolder);
