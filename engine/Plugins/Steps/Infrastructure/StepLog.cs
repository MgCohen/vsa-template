namespace Steps.Infrastructure;

using Steps.Contracts.Commands;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;

public sealed class StepLog {
    readonly List<Finding> findings = [];

    public bool FailFast { get; set; }

    public IReadOnlyList<Finding> Findings => findings;

    public void Add(IEnumerable<Finding> added) => findings.AddRange(added);

    public Result<TResult> Skip<TStep, TResult>() where TStep : EngineStep<TResult> {
        var skipped = new Error(RunCodes.Skipped, $"{StepNames.Of(typeof(TStep))} skipped, because an earlier step failed.");
        findings.Add(new Finding(skipped.Code, FindingSeverity.Info, skipped.Message));
        return skipped;
    }
}
