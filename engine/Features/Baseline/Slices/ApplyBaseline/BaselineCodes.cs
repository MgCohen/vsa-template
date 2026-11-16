namespace Baseline.Slices.ApplyBaseline;

using Steps.Domain.Shared;

public static class BaselineCodes {
    public static readonly string StaleEntry = StepCode.Of<ApplyBaselineCommand>(1);
    public static readonly string UnreadableEntry = StepCode.Of<ApplyBaselineCommand>(2);
    public static readonly string NotUpdated = StepCode.Of<ApplyBaselineCommand>(3);
}
