namespace Umpire.Cli.Features.Checks.Infrastructure;

using System.Text.Json;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Infrastructure;

sealed record AppEngine(string File, string RollForward, string Bin, string Block) {
    const string EngineFile = "UmpireEngineFile";
    const string EngineRollForward = "UmpireEngineRollForward";
    const string TargetDir = "TargetDir";
    const string UmpireBlock = "UmpireBlock";
    const string DefaultRollForward = "LatestMajor";

    static readonly string[] properties = [EngineFile, EngineRollForward, TargetDir, UmpireBlock];

    public static AppEngine Of(string block, string? configuration) {
        var project = Path.Combine(block, RootProject.Name(block) + ".csproj");
        string[] build = configuration is null ? [] : [$"-p:Configuration={configuration}"];
        var read = Dotnet.Run(block, [
            "msbuild", project, "-restore", "-nologo", "-nodeReuse:false",
            .. properties.Select(name => $"-getProperty:{name}"), .. build]);
        if (read.ExitCode != 0) {
            throw new UmpireException($"The restore of {project} failed with exit code {read.ExitCode}. "
                + $"Fix the restore, then run 'umpire check' again.{Environment.NewLine}{read.Output.Trim()}");
        }
        return FromProperties(block, read.Output);
    }

    public IReadOnlyList<string> CheckArguments(string block, string? mode) {
        string[] blockArgument = Block.Length > 0 ? ["--block", Trimmed(Block)] : [];
        string[] modeArgument = mode is null ? [] : ["--mode", mode];
        return ["exec", "--roll-forward", RollForward, File, "jobs", "check",
            "--project", Trimmed(block), "--bin", Trimmed(Bin), .. blockArgument, .. modeArgument];
    }

    internal static AppEngine FromProperties(string block, string output) {
        var start = output.IndexOf('{');
        if (start < 0) {
            throw new UmpireException($"MSBuild gave no properties for the app in {block}: {output.Trim()}");
        }
        var values = Parse(block, output[start..]);
        var engine = new AppEngine(
            Value(values, EngineFile),
            Value(values, EngineRollForward) is { Length: > 0 } rollForward ? rollForward : DefaultRollForward,
            Value(values, TargetDir),
            Value(values, UmpireBlock));
        engine.EnsureFound();
        return engine;
    }

    static JsonElement Parse(string block, string json) {
        try {
            return JsonDocument.Parse(json).RootElement.GetProperty("Properties").Clone();
        } catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException) {
            throw new UmpireException($"MSBuild gave no properties for the app in {block}: {json.Trim()}");
        }
    }

    static string Value(JsonElement values, string name) =>
        values.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

    static string Trimmed(string folder) => Path.GetFullPath(folder).TrimEnd('/', '\\');

    void EnsureFound() {
        if (File.Length == 0) {
            throw new UmpireException("The app has no rule engine, so 'umpire check' cannot run. "
                + "Add the package Umpire.Engine to the root project: "
                + "<PackageReference Include=\"Umpire.Engine\" Version=\"<version>\" PrivateAssets=\"all\" />.");
        }
        if (Bin.Length == 0) {
            throw new UmpireException("The root project has no single output folder, so 'umpire check' cannot run. "
                + "Give the root project one TargetFramework.");
        }
        if (!System.IO.File.Exists(File)) {
            throw new UmpireException($"The rule engine {File} does not exist. "
                + "Restore the app with 'dotnet restore', then run 'umpire check' again.");
        }
    }
}
