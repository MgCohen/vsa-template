using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;

namespace Rules.Slices.CheckRules;

public sealed record CheckRulesCommand(AppInput App) : EngineStep<CheckRulesResult>;
