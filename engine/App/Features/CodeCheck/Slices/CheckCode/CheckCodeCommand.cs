namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record CheckCodeCommand(CompileProject Project, CompileStage Stage, CodeFile? File = null, CodeType? Type = null)
    : JobOperation<CheckCodeResult>, IOperationGroup {
    public static int Group => 9;
}
