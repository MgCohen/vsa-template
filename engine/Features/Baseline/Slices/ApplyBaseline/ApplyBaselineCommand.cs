using Steps.Contracts.Commands;
using Steps.Domain.Shared;

namespace Baseline.Slices.ApplyBaseline;

public sealed record ApplyBaselineCommand(string AppRoot, bool Update, IReadOnlyList<Finding> Findings)
    : EngineStep<ApplyBaselineResult>;
