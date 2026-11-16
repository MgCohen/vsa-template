namespace Rules.Slices.CheckRules;

using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record CheckRulesCommand(AppInput App) : EngineStep<CheckRulesResult>, IStepGroup {
    public static int Group => 3;
}
