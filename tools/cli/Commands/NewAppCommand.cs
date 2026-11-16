namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

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
        var files = Files(name, input.All(Source), version);
        Console.WriteLine($"Making the app {name} (Umpire {version}):");
        FileWriter.WriteAllOrNone(block, files);

        var (code, output) = Dotnet.Run(block, "tool", "restore");
        if (code != 0) {
            throw new UmpireException(
                $"The app is written, but 'dotnet tool restore' failed. Fix the error, then run it in {name}."
                + $"{Environment.NewLine}{output}");
        }
        Console.WriteLine($"""

            Next:
              cd {name}
              dotnet umpire add module <Name>
            """);
        return 0;
    }

    internal static IReadOnlyList<TemplateFile> Files(string name, IReadOnlyList<string> sources, string version) =>
        Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = name }, version)
            .Select(file => file.Path == NugetConfig.FileName
                ? file with { Content = NugetConfig.Write(file.Content, sources) }
                : file)
            .ToList();
}
