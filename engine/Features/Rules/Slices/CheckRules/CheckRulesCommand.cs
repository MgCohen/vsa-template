using Definition.Domain.Shared;
using Steps.Contracts.Commands;

namespace Rules.Slices.CheckRules;

public sealed record CheckRulesCommand(LoadedApp App) : EngineStep<CheckRulesResult>;
