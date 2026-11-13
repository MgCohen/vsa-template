using VSA.Contracts.Commands;

namespace Definition.Slices.LoadDefinition;

public sealed record LoadDefinitionCommand(string AppRoot, string Bin, IReadOnlyList<string> Skip)
    : ICommand<LoadDefinitionResult>;
