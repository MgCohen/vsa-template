namespace Tests.Shared;

using System.Diagnostics;
using System.Text;

public sealed record DotnetResult(string Command, int ExitCode, string Output) {
    public bool Succeeded => ExitCode == 0;

    public DotnetResult EnsureSuccess() =>
        Succeeded ? this : throw new InvalidOperationException($"'{Command}' failed with exit code {ExitCode}:\n{Output}");
}

public static class Dotnet {
    public static DotnetResult Run(string workDir, IReadOnlyDictionary<string, string>? env, params string[] args) {
        using var process = Start(workDir, env, args);
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return new DotnetResult($"dotnet {string.Join(' ', args)}", process.ExitCode, output.ToString());
    }

    public static Process Start(string workDir, IReadOnlyDictionary<string, string>? env, params string[] args) {
        var info = new ProcessStartInfo("dotnet") {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args) {
            info.ArgumentList.Add(arg);
        }
        foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList()) {
            info.Environment.Remove(key);
        }
        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        info.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        info.Environment["DOTNET_NOLOGO"] = "1";
        info.Environment["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "0";
        foreach (var (key, value) in env ?? new Dictionary<string, string>()) {
            info.Environment[key] = value;
        }
        return Process.Start(info) ?? throw new InvalidOperationException("dotnet did not start.");
    }
}
