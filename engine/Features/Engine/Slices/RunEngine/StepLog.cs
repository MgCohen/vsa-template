using Steps.Domain.Shared;

namespace Engine.Slices.RunEngine;

public sealed class StepLog {
    readonly List<Finding> findings = [];

    public bool FailFast { get; set; }

    public IReadOnlyList<Finding> Findings => findings;

    public void Add(IEnumerable<Finding> added) => findings.AddRange(added);
}
