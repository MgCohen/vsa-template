using LoadApp.Domain.Shared;
using LoadTemplates.Domain.Shared;
using Scaffold.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

namespace PlanScaffold.Slices.PlanScaffold;

public sealed record PlanScaffoldCommand(AppInput App, TemplatesInput Templates, ScaffoldInput Input)
    : EngineStep<ScaffoldPlan>, IStepGroup {
    public static int Group => 8;
}
