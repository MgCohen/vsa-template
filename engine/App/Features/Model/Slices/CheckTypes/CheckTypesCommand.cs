namespace Model.Slices.CheckTypes;

using Facts.Domain.Shared.Compile;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record CheckTypesCommand(CompileProject Project, CompileStage Stage, CodeType? Type = null, bool HasErrors = false)
    : JobOperation<CheckTypesResult>, IOperationGroup {
    public static int Group => 10;
}
