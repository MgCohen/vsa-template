namespace Umpire.Cli.Features.Scaffold.Domain.Setup.Tasks;

using Umpire.Files;

sealed class BuildPropsTask : SetupTask {
    const string FileName = "Directory.Build.props";

    public override string Id => "build-props";

    public override SetupResult Plan(SetupContext context) =>
        context.Read(FileName) is null
            ? new SetupResult.Change(new TextFile(FileName, Templates.SetupFile(FileName, context.Version)))
            : new SetupResult.Done();
}
