namespace PlanScaffold.Slices.PlanScaffold;

using Steps.Domain.Shared;

public static class PlanScaffoldCodes {
    public static readonly string UnknownKind = StepCode.Of<PlanScaffoldCommand>(1);
    public static readonly string InvalidName = StepCode.Of<PlanScaffoldCommand>(2);
    public static readonly string MissingGroupValue = StepCode.Of<PlanScaffoldCommand>(3);
    public static readonly string NoGroupFolder = StepCode.Of<PlanScaffoldCommand>(4);
    public static readonly string FilesExist = StepCode.Of<PlanScaffoldCommand>(5);
}
