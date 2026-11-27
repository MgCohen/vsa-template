namespace Operations.Infrastructure;

using Operations.Domain.Shared;
using Umpire.Messages.Contracts;

public sealed class OperationLog {
    readonly List<Finding> findings = [];

    public IReadOnlyList<Finding> Findings => findings;

    public void Add(IEnumerable<Finding> added) => findings.AddRange(added);

    public bool OnlyInfoSince(int count) => findings.Skip(count).All(f => f.Severity == FindingSeverity.Info);

    public Result<TResult> Skip<TOperation, TResult>() where TOperation : IJobOperation {
        var skipped = new Error(
            RunCodes.Skipped, $"{OperationNames.Of(typeof(TOperation))} skipped, because an earlier operation failed.");
        findings.Add(new Finding(skipped.Code, FindingSeverity.Info, skipped.Message));
        return skipped;
    }
}
