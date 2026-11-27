namespace Umpire.Cli.Features.Scaffold.Domain.Setup.Tasks;

using Umpire.Files;

sealed class GitIgnoreTask : SetupTask {
    const string FileName = ".gitignore";

    public override string Id => "git-ignore";

    public override SetupResult Plan(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.Read(FileName) is not string text) {
            return new SetupResult.Change(new TextFile(FileName, template));
        }
        var lines = text.Split('\n').Select(line => line.Trim()).ToHashSet(StringComparer.Ordinal);
        var missing = template.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !lines.Contains(line))
            .ToList();
        if (missing is []) {
            return new SetupResult.Done();
        }
        var end = text is "" || text.EndsWith('\n') ? "" : "\n";
        return new SetupResult.Change(new TextFile(FileName, text + end + string.Join('\n', missing) + "\n"));
    }
}
