using System.Diagnostics;

namespace VSA.Cli.Scaffolding;

static class Dotnet {
    // Runs the dotnet CLI in the folder. Returns the exit code and the output.
    public static (int ExitCode, string Output) Run(string folder, params string[] args) {
        var start = new ProcessStartInfo("dotnet") {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in args) {
            start.ArgumentList.Add(arg);
        }
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output + error.Result);
    }
}
