namespace Umpire.Cli.Scaffolding.Setup;

using Umpire.Cli.Scaffolding.Setup.Steps;

static class SetupPipeline {
    public static IReadOnlyList<SetupStep> Default { get; } = [
        new GlobalJsonStep(),
        new NugetConfigStep(),
        new ToolManifestStep(),
        new BuildOutputStep(),
        new GitIgnoreStep(),
        new UmpireSdkStep(),
        new BuildPackageStep(),
    ];

    public static SetupPlan Plan(SetupContext context) {
        var results = new List<StepResult>();
        foreach (var step in Default) {
            var result = step.Plan(context);
            if (result is SetupResult.Change change) {
                context.Add(new TemplateFile(change.File, change.Content));
            }
            results.Add(new StepResult(step.Id, result));
        }
        return new SetupPlan(context, results);
    }
}
