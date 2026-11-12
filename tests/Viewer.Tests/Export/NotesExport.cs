using System.Diagnostics;
using System.Reflection;

namespace Viewer.Tests.Export;

public static class NotesExport
{
    private static readonly Lazy<string> file = new(Make);

    public static string File => file.Value;

    public static string Json => System.IO.File.ReadAllText(File);

    private static string Make()
    {
        string output = Path.Combine(Path.GetTempPath(), $"vsa-viewer-{Environment.ProcessId}", "architecture.json");
        ProcessStartInfo start = new("dotnet") { UseShellExecute = false, RedirectStandardOutput = true };
        foreach (string argument in new[] { Metadata("ViewerExport"), Metadata("NotesOutput"), Metadata("NotesRoot") })
        {
            start.ArgumentList.Add(argument);
        }
        start.ArgumentList.Add(output);
        using Process process = Process.Start(start)!;
        string log = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Viewer.Export stopped with exit code {process.ExitCode}. {log}");
        }
        return output;
    }

    private static string Metadata(string key)
    {
        return typeof(NotesExport).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == key).Value!;
    }
}
