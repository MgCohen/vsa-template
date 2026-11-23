namespace WriteScaffold.Slices.WriteScaffold;

using Scaffold.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record WriteScaffoldCommand(string BlockFolder, ScaffoldPlan Plan)
    : EngineStep<WriteScaffoldResult>, IStepGroup {
    public static int Group => 9;
}
