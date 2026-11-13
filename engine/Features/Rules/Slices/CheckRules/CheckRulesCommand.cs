using System.Reflection;
using VSA.Architecture;
using VSA.Contracts.Commands;

namespace Rules.Slices.CheckRules;

public sealed record CheckRulesCommand(ArchitectureDefinition Definition, IReadOnlyList<Assembly> Assemblies, string AppRoot)
    : ICommand<CheckRulesResult>;
