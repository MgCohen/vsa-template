namespace Build.Slices.BuildApp;

using Build.Domain.Shared;
using Build.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class BuildAppHandler : CommandHandler<BuildAppCommand, BuildAppResult> {
    const int ShownLines = 20;

    public override async Task<Result<BuildAppResult>> HandleAsync(Envelope<BuildAppCommand> envelope) {
        var project = envelope.Message.Project;
        var build = await DotnetBuild.RunAsync(project, envelope.Cancellation);
        return build.ExitCode == 0
            ? new BuildAppResult([])
            : new Error(BuildCodes.Failed, $"The build of {project} failed with exit code {build.ExitCode}:\n{Shown(build.Lines)}");
    }

    static string Shown(IReadOnlyList<string> lines) {
        var errors = lines.Where(line => line.Contains(": error ", StringComparison.Ordinal)).Distinct().ToList();
        return string.Join("\n", errors.Count > 0 ? errors.Take(ShownLines) : lines.TakeLast(ShownLines));
    }
}
