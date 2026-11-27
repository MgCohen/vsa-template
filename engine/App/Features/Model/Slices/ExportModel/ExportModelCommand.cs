namespace Model.Slices.ExportModel;

using Facts.Domain.Shared;
using LoadApp.Domain.Shared;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record ExportModelCommand(AppInput App, BinIndex Bin, string State, string Snapshots, ReportMode Mode)
    : JobOperation<ExportModelResult>, IOperationGroup {
    public static int Group => 7;
}
