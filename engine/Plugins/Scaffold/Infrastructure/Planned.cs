namespace Scaffold.Infrastructure;

using Scaffold.Domain.Shared;
public sealed record Planned(ScaffoldPlan? Plan, ScaffoldProblem? Problem) {
    public static implicit operator Planned(ScaffoldPlan plan) => new(plan, null);

    public static implicit operator Planned(ScaffoldProblem problem) => new(null, problem);
}
