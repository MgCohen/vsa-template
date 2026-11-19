namespace Changes.Slices.FindChanges;

using Steps.Domain.Shared;

public static class ChangesCodes {
    public static readonly string Same = StepCode.Of<FindChangesCommand>(1);
}
