namespace Umpire.Cli.Commands;

using Umpire.Engine.Versions;
using Umpire.Cli.Commands.Bases;
using Umpire.Files;

sealed class UpdateCommand : AppCommand {
    static readonly CommandOption Version = new(["--version"], "version");

    public override IReadOnlyList<string> Words => ["update"];

    public override string Summary =>
        "Moves the Umpire SDKs and the Umpire packages of the app to the version of this tool, or to <version>.";

    public override IReadOnlyList<CommandOption> Options => [Version];

    internal static IReadOnlyList<string> SetVersion(string app, string version) {
        var changed = new List<string>();
        foreach (var file in VersionSlots.InApp(app)) {
            if (file.SetVersion(version)) {
                new TextFile(file.File, file.Content).Write(app);
                changed.Add(file.File);
            }
        }
        return changed;
    }

    protected override int Run(string block, CommandInput input) {
        var target = input.Single(Version) ?? ToolVersion.Current;
        var changed = SetVersion(block, target);
        Console.WriteLine($"Umpire {target}:");
        foreach (var file in changed) {
            Console.WriteLine($"  {file}");
        }
        Console.WriteLine(changed is []
            ? "The files already use this version."
            : $"Changed {changed.Count} files. Next: dotnet build");
        return 0;
    }
}
