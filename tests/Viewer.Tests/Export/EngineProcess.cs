using System.Diagnostics;
using System.Reflection;

namespace Viewer.Tests.Export;

public static class EngineProcess
{
    public static DotnetRun Run(string root, string bin, params string[] arguments)
    {
        ProcessStartInfo start = new("dotnet") { UseShellExecute = false, RedirectStandardOutput = true };
        string[] app = ["exec", Metadata("Engine"), "--root", root, "--bin", bin];
        foreach (string argument in app.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        string log = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new DotnetRun(process.ExitCode, log);
    }

    public static string Metadata(string key)
    {
        return typeof(EngineProcess).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == key).Value!;
    }
}
