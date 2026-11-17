namespace Umpire.Cli.Scaffolding.Setup;

using Umpire.Cli.Scaffolding.Setup.Tasks;

static class SetupTasks {
    public static IReadOnlyList<SetupTask> Default { get; } = [
        new GlobalJsonTask(),
        new NugetConfigTask(),
        new GitIgnoreTask(),
        new UmpireSdkTask(),
        new UmpireConfigTask(),
    ];

    public static SetupPlan Plan(SetupContext context) {
        var results = new List<TaskResult>();
        foreach (var task in Default) {
            var result = task.Plan(context);
            if (result is SetupResult.Change change) {
                context.Add(new TemplateFile(change.File, change.Content));
            }
            results.Add(new TaskResult(task.Id, result));
        }
        return new SetupPlan(context, results);
    }
}
