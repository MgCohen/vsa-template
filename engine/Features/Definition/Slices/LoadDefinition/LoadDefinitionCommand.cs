using Definition.Domain.Shared;
using Steps.Contracts.Commands;
using Versions.Domain.Shared;

namespace Definition.Slices.LoadDefinition;

public sealed record LoadDefinitionCommand(
    string AppRoot, string Bin, string Skip, CheckVersionsResult CheckedVersions)
    : EngineStep<AppInput>;
