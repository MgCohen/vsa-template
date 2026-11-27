namespace Model.Slices.CheckModel;

using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record CheckModelCommand(string Block, string State, ReportMode? Mode)
    : JobOperation<CheckModelResult>, IOperationGroup {
    public static int Group => 8;
}
