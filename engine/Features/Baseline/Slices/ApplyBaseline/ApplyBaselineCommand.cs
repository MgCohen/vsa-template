namespace Baseline.Slices.ApplyBaseline;

using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record ApplyBaselineCommand(string AppRoot, bool Update, IReadOnlyList<Finding> Findings)
    : EngineStep<ApplyBaselineResult>, IStepGroup {
    public static int Group => 4;
}
