namespace Rules.Slices.CheckRules;

using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;

public sealed record CheckRulesCommand(AppInput App) : EngineStep<CheckRulesResult>;
