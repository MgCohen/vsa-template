namespace Rules.Slices.CheckRules;

using LoadApp.Domain.Shared;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record CheckRulesCommand(AppInput App) : JobOperation<CheckRulesResult>, IOperationGroup {
    public static int Group => 6;
}
