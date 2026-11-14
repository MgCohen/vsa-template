using System.Diagnostics;
using System.Text;

namespace Tests.Shared;

public sealed record DotnetResult(string Command, int ExitCode, string Output) {
    public bool Succeeded => ExitCode == 0;

    public DotnetResult EnsureSuccess() =>
        Succeeded ? this : throw new InvalidOperationException($"'{Command}' failed with exit code {ExitCode}:\n{Output}");
}

// Runs the dotnet CLI in a child process.
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

    // The caller reads the output and stops the process.
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
        // dotnet test sets MSBuild variables for its own build. A child build must not use them.
        foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList()) {
            info.Environment.Remove(key);
        }
        // No build server or node stays alive, so a temporary folder can be deleted.
        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        info.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        info.Environment["DOTNET_NOLOGO"] = "1";
        // A test can set its own DOTNET_CLI_HOME. On the first run in a new home, the CLI adds <home>\.dotnet\tools to
        // the user PATH in the registry. The test deletes the folder, but the PATH entry stays.
        info.Environment["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "0";
        foreach (var (key, value) in env ?? new Dictionary<string, string>()) {
            info.Environment[key] = value;
        }
        return Process.Start(info) ?? throw new InvalidOperationException("dotnet did not start.");
    }
}
