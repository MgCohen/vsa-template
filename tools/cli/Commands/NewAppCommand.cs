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
        var plan = Plan(name, block, input.All(Source), version);
        if (plan.ByHand is [var first, ..]) {
            throw new UmpireException(
                $"The folder {name} has a setup that the tool cannot complete ({first}). "
                + "To add Umpire to an existing app, run 'umpire init' in its folder.");
        }
        Console.WriteLine($"Making the app {name} (Umpire {version}):");
        FileWriter.WriteAllOrNone(block, plan.Files);
        Dotnet.RestoreTools(block, name);
        Console.WriteLine($"""

            Next:
              cd {name}
              dotnet umpire add module <Name>
            """);
        return 0;
    }

    internal static IReadOnlyList<TemplateFile> Files(string name, IReadOnlyList<string> sources, string version) =>
        Plan(name, Path.GetFullPath(name), sources, version).Files;

    static SetupPlan Plan(string name, string block, IReadOnlyList<string> sources, string version) {
        var context = new SetupContext(block, $"{name}.csproj", version, sources, keepsParentFiles: false);
        var names = new Dictionary<string, string> { [Templates.App] = name };
        foreach (var file in Templates.Files(Templates.Kind.App, names, version)) {
            context.Add(file);
        }
        return SetupPipeline.Plan(context);
    }
}
