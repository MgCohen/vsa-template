using VSA.Architecture.Versions;
using VSA.Cli.Commands.Bases;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

sealed class UpdateCommand : AppCommand {
    const string AddTool = "Add it with 'dotnet tool install vsa.cli --local'.";
    static readonly CommandOption Version = new(["--version"], "version");

    public override IReadOnlyList<string> Words => ["update"];

    public override string Summary =>
        "Moves the tool, VSA.Sdk and the packages to the latest version, or to <version>.";

    public override IReadOnlyList<CommandOption> Options => [Version];

    internal static IReadOnlyList<string> SetVersion(string root, string version) {
        var changed = new List<string>();
        foreach (var file in VersionSlots.InApp(root)) {
            if (file.SetVersion(version)) {
                changed.Add(file.File);
            }
        }
        return changed;
    }

    protected override int Run(string root, CommandInput input) {
        var version = input.Single(Version);
        var manifest = Manifest(root);
        var manifestPath = Path.Combine(root, manifest);
        var current = VersionSlots.ManifestToolVersion(manifestPath)
            ?? throw new VsaException($"{manifest} has no vsa.cli. {AddTool}");

        if (version != current) {
            UpdateTool(root, version, current);
        }
        var target = VersionSlots.ManifestToolVersion(manifestPath)!;

        var changed = SetVersion(root, target);
        Console.WriteLine(current == target
            ? $"The tool has VSA {target}."
            : $"The tool changed from VSA {current} to {target}.");
        foreach (var file in changed) {
            Console.WriteLine($"  {file}");
        }
        Console.WriteLine(changed is []
            ? "The files already use this version."
            : $"Changed {changed.Count} files. Next: dotnet build");
        return 0;
    }

    static string Manifest(string root) => VersionSlots.ToolManifests(root) switch {
        [var one] => one,
        [] => throw new VsaException($"The app has no tool manifest (dotnet-tools.json) with vsa.cli. {AddTool}"),
        _ => throw new VsaException(
            "The app has two tool manifests: dotnet-tools.json and .config/dotnet-tools.json. Keep one."),
    };

    static void UpdateTool(string root, string? version, string current) {
        string[] prerelease = current.Contains('-') ? ["--prerelease"] : [];
        string[] args = version is null
            ? ["tool", "update", "vsa.cli", "--local", .. prerelease]
            : ["tool", "update", "vsa.cli", "--local", "--version", version, "--allow-downgrade"];
        var (code, output) = Dotnet.Run(root, args);
        if (code != 0) {
            throw new VsaException(
                $"'dotnet {string.Join(' ', args)}' failed. The app did not change.{Environment.NewLine}{output}");
        }
    }
}
