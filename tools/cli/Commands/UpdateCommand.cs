namespace Umpire.Cli.Commands;

using Umpire.Engine.Versions;
using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class UpdateCommand : AppCommand {
    const string AddTool = "Add it with 'dotnet tool install umpire.cli --local'.";
    static readonly CommandOption Version = new(["--version"], "version");

    public override IReadOnlyList<string> Words => ["update"];

    public override string Summary =>
        "Moves the tool, Umpire.Sdk and the packages to the latest version, or to <version>.";

    public override IReadOnlyList<CommandOption> Options => [Version];

    internal static IReadOnlyList<string> SetVersion(string block, string version) {
        var changed = new List<string>();
        foreach (var file in VersionSlots.InApp(block)) {
            if (file.SetVersion(version)) {
                changed.Add(file.File);
            }
        }
        return changed;
    }

    protected override int Run(string block, CommandInput input) {
        var version = input.Single(Version);
        var manifest = Manifest(block);
        var manifestPath = Path.Combine(block, manifest);
        var current = VersionSlots.ManifestToolVersion(manifestPath)
            ?? throw new UmpireException($"{manifest} has no umpire.cli. {AddTool}");

        if (version != current) {
            UpdateTool(block, version, current);
        }
        var target = VersionSlots.ManifestToolVersion(manifestPath)!;

        var changed = SetVersion(block, target);
        Console.WriteLine(current == target
            ? $"The tool has Umpire {target}."
            : $"The tool changed from Umpire {current} to {target}.");
        foreach (var file in changed) {
            Console.WriteLine($"  {file}");
        }
        Console.WriteLine(changed is []
            ? "The files already use this version."
            : $"Changed {changed.Count} files. Next: dotnet build");
        return 0;
    }

    static string Manifest(string block) => VersionSlots.ToolManifests(block) switch {
        [var one] => one,
        [] => throw new UmpireException($"The app has no tool manifest (dotnet-tools.json) with umpire.cli. {AddTool}"),
        _ => throw new UmpireException(
            "The app has two tool manifests: dotnet-tools.json and .config/dotnet-tools.json. Keep one."),
    };

    static void UpdateTool(string block, string? version, string current) {
        string[] prerelease = current.Contains('-') ? ["--prerelease"] : [];
        string[] args = version is null
            ? ["tool", "update", "umpire.cli", "--local", .. prerelease]
            : ["tool", "update", "umpire.cli", "--local", "--version", version, "--allow-downgrade"];
        var run = Dotnet.Run(block, args);
        if (run.ExitCode != 0) {
            throw new UmpireException(
                $"'dotnet {string.Join(' ', args)}' failed. The app did not change.{Environment.NewLine}{run.Output}");
        }
    }
}
