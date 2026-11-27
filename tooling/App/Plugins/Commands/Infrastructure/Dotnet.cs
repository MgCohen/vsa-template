namespace Umpire.Cli.Plugins.Commands.Infrastructure;

using System.Diagnostics;

public static class Dotnet {
    public static readonly TimeSpan StopTime = TimeSpan.FromSeconds(10);

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

    public static Task<int> Stream(string folder, IReadOnlyList<string> args, CancellationToken ct) =>
        ChildProcess.Stream(new ProcessStartInfo("dotnet") { WorkingDirectory = folder }, args, StopTime, ct);
}
