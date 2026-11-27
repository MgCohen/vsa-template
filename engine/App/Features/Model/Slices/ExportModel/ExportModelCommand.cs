namespace Model.Slices.ExportModel;

using Operations.Contracts.Commands;
using Operations.Domain.Shared;
using Umpire.Architecture;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;

public sealed record ExportModelCommand(
    StateFolder State, ResolvedArchitecture Definition, BlockInput Block, IReadOnlyList<Snapshot> Libraries)
    : JobOperation<ExportModelResult>, IOperationGroup {
    public static int Group => 7;
}
