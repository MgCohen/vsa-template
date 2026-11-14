using Definition.Domain.Shared;
using Steps.Contracts.Commands;

namespace Definition.Slices.LoadDefinition;

public sealed record LoadDefinitionCommand(string AppRoot, string Bin, string Skip) : EngineStep<LoadedApp>;
