namespace Build.Slices.BuildApp;

using Build.Domain.Shared;
using Build.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;
using BuiltApp.Domain.Shared;
using Umpire.Vsa.Handlers.Commands;

sealed class BuildAppHandler : CommandHandler<BuildAppCommand, BuildAppResult> {
    const int ShownLines = 20;

    public override async Task<Result<BuildAppResult>> HandleAsync(IEnvelope<BuildAppCommand> envelope) {
        var project = envelope.Message.Project;
        if (UnityFolder.Root(Path.GetFullPath(project)) is { } unity) {
            return new BuildAppResult([new Finding(BuildCodes.UnityCompiles, FindingSeverity.Info,
                $"{unity} is a Unity project. Unity compiles it, so the engine does not build it. Compile it in Unity first.")]);
        }
        var configuration = BuildConfiguration.Of(envelope.Message.Bin);
        var build = await DotnetBuild.RunAsync(project, configuration, envelope.Cancellation);
        var findings = BuildFindings.Of(build.Lines);
        if (build.ExitCode == 0) {
            return new BuildAppResult(findings);
        }
        var shown = findings.Any(f => f.Severity == FindingSeverity.Error)
            ? ""
            : "\n" + string.Join("\n", build.Lines.TakeLast(ShownLines));
        var failed = new Finding(BuildCodes.Failed, FindingSeverity.Error,
            $"The build of {project} failed with exit code {build.ExitCode}.{shown}");
        return new BuildAppResult([.. findings, failed], Failed: true);
    }
}
