namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;
using Umpire.Cli.Scaffolding.Setup;

sealed class NewAppCommand : ToolCommand {
    static readonly CommandOption Source = new(["--source"], "feed", Many: true);

    public override IReadOnlyList<string> Words => ["new", "app"];

    public override string Summary =>
        "Makes a new app in the folder <Name>. Each --source replaces the GitHub feed of the Umpire packages.";

    public override bool TakesName => true;

    public override IReadOnlyList<CommandOption> Options => [Source];

    public override int Run(CommandInput input) {
        var name = Names.Check(input.Name, "app");
        input.EnsureRequiredOptions();
        var block = Path.GetFullPath(name);
        var version = ToolVersion.Current;
        var context = Context(name, block, input.All(Source), version);
        var existing = SetupTasks.Default.SelectMany(task => task.Files(context))
            .Distinct()
            .Where(context.OnDisk)
            .ToList();
        if (existing is not []) {
            var list = string.Join(Environment.NewLine + "  ", existing);
            throw new UmpireException(
                $"These files exist. The tool does not write over a file:{Environment.NewLine}  {list}"
                + $"{Environment.NewLine}To add Umpire to an existing app, run 'umpire init' in its folder.");
        }
        var plan = SetupTasks.Plan(context);
        if (plan.ByHand is [var first, ..]) {
            throw new UmpireException(
                $"The folder {name} has a setup that the tool cannot complete ({first}). "
                + "To add Umpire to an existing app, run 'umpire init' in its folder.");
        }
        Console.WriteLine($"Making the app {name} (Umpire {version}):");
        FileWriter.WriteAllOrNone(block, plan.Files);
        Console.WriteLine($"""

            Next:
              cd {name}
              dotnet tool exec Umpire.Cli@{version} -y -- add module <Name>
            """);
        return 0;
    }

    internal static IReadOnlyList<TemplateFile> Files(string name, IReadOnlyList<string> sources, string version) =>
        SetupTasks.Plan(Context(name, Path.GetFullPath(name), sources, version)).Files;

    static SetupContext Context(string name, string block, IReadOnlyList<string> sources, string version) {
        var context = new SetupContext(block, $"{name}.csproj", version, sources, keepsParentFiles: false);
        var names = new Dictionary<string, string> { [Templates.App] = name };
        foreach (var file in Templates.Files(TemplateGroup.App, names, version)) {
            context.Add(file);
        }
        return context;
    }
}
