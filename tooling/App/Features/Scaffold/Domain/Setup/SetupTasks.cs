namespace Umpire.Cli.Features.Scaffold.Domain.Setup;

using Umpire.Cli.Features.Scaffold.Domain.Setup.Tasks;

static class SetupTasks {
    public static IReadOnlyList<SetupTask> Default { get; } = [
        new GlobalJsonTask(),
        new NugetConfigTask(),
        new GitIgnoreTask(),
        new BuildPropsTask(),
        new UmpirePackagesTask(),
        new UmpireConfigTask(),
    ];

    public static SetupPlan Plan(SetupContext context) {
        var results = new List<TaskResult>();
        foreach (var task in Default) {
            var result = task.Plan(context);
            if (result is SetupResult.Change change) {
                context.Add(change.File);
            }
            results.Add(new TaskResult(task.Id, result));
        }
        return new SetupPlan(context, results);
    }
}
