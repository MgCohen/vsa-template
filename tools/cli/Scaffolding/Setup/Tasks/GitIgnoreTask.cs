namespace Umpire.Cli.Scaffolding.Setup.Tasks;

sealed class GitIgnoreTask : SetupTask {
    const string FileName = ".gitignore";

    public override string Id => "git-ignore";

    public override IReadOnlyList<string> Files(SetupContext context) => [FileName];

    public override SetupResult Plan(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.Read(FileName) is not string text) {
            return Change(FileName, template);
        }
        var lines = text.Split('\n').Select(line => line.Trim()).ToHashSet(StringComparer.Ordinal);
        var missing = template.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !lines.Contains(line))
            .ToList();
        if (missing is []) {
            return Done();
        }
        var end = text is "" || text.EndsWith('\n') ? "" : "\n";
        return Change(FileName, text + end + string.Join('\n', missing) + "\n");
    }
}
