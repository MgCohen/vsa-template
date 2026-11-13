using System.Diagnostics;
using System.Reflection;

namespace VSA.Testing.Architecture;

public static class RuleEngine {
    public const string EngineKey = "VsaEngine";

    public static RuleEngineRun? Run() {
        var entry = Assembly.GetEntryAssembly();
        var engine = Metadata(entry, EngineKey);
        if (entry is null || string.IsNullOrEmpty(engine)) {
            return null;
        }
        var root = Metadata(entry, "VsaAppRoot") is { Length: > 0 } given ? given : GlobalJsonFolder(AppContext.BaseDirectory);
        return Run(engine, root, AppContext.BaseDirectory, entry.GetName().Name!);
    }

    public static RuleEngineRun Run(string engine, string appRoot, string bin, string skip = "") {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] {
            "exec", engine, "--root", Path.TrimEndingDirectorySeparator(appRoot), "--bin", Path.TrimEndingDirectorySeparator(bin),
            "--skip", skip,
        }) {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"The rule engine {engine} did not start.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new RuleEngineRun(process.ExitCode, lines, error.Result.Trim());
    }

    static string? Metadata(Assembly? assembly, string key) =>
        assembly?.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value;

    static string GlobalJsonFolder(string start) {
        for (var folder = Path.GetFullPath(start); folder is not null; folder = Path.GetDirectoryName(folder)) {
            if (File.Exists(Path.Combine(folder, "global.json"))) {
                return folder;
            }
        }
        throw new InvalidOperationException($"No global.json above {start}. Add global.json to the app root.");
    }
}
