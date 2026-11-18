namespace Build.Infrastructure;

using System.Diagnostics;

public static class DotnetBuild {
    const string HostPath = "DOTNET_HOST_PATH";
    const string MSBuildPrefix = "MSBuild";

    public static async Task<BuildOutput> RunAsync(string project, string? configuration, CancellationToken cancellation) {
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable(HostPath) ?? "dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        string[] build = configuration is null ? [] : ["-c", configuration];
        foreach (var argument in new[] { "build", project, "-nologo", "-nodeReuse:false" }.Concat(build)) {
            info.ArgumentList.Add(argument);
        }
        foreach (var key in info.Environment.Keys.Where(IsMSBuildValue).ToList()) {
            info.Environment.Remove(key);
        }
        using var process = Process.Start(info) ?? throw new InvalidOperationException("dotnet did not start.");
        var output = process.StandardOutput.ReadToEndAsync(cancellation);
        var errors = process.StandardError.ReadToEndAsync(cancellation);
        await process.WaitForExitAsync(cancellation);
        var lines = (await output + await errors).Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        return new BuildOutput(process.ExitCode, lines);
    }

    static bool IsMSBuildValue(string key) => key.StartsWith(MSBuildPrefix, StringComparison.OrdinalIgnoreCase);
}
