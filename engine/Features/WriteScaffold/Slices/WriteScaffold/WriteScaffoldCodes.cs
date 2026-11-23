namespace WriteScaffold.Slices.WriteScaffold;

using Steps.Domain.Shared;

public static class WriteScaffoldCodes {
    public static readonly string Created = StepCode.Of<WriteScaffoldCommand>(1);
}
