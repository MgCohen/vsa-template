namespace Umpire.Cli.Scaffolding;

using System.Diagnostics;

static class Dotnet {
    public static DotnetRun Run(string folder, params string[] args) {
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
        return new DotnetRun(process.ExitCode, output + error.Result);
    }

    public static void RestoreTools(string block, string shownFolder) {
        var (code, output) = Run(block, "tool", "restore");
        if (code != 0) {
            throw new UmpireException(
                $"The files are written, but 'dotnet tool restore' failed. Fix the error, then run it in {shownFolder}."
                + $"{Environment.NewLine}{output}");
        }
    }
}
