using System.Diagnostics;
using System.Reflection;

namespace Viewer.Tests.Export;

public static class NotesExport
{
    private static readonly Lazy<string> folder = new(Make);

    public static string Folder => folder.Value;

    public static string File => Path.Combine(Folder, "architecture.json");

    public static string Json => System.IO.File.ReadAllText(File);

    public static string DefinitionFile => Path.Combine(Folder, "definition.json");

    public static string DefinitionJson => System.IO.File.ReadAllText(DefinitionFile);

    public static DotnetRun Engine(params string[] arguments)
    {
        ProcessStartInfo start = new("dotnet") { UseShellExecute = false, RedirectStandardOutput = true };
        string[] app = ["exec", Metadata("Engine"), "--root", Metadata("NotesRoot"), "--bin", Metadata("NotesOutput")];
        foreach (string argument in app.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        string log = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new DotnetRun(process.ExitCode, log);
    }

    private static string Make()
    {
        string output = Path.Combine(Path.GetTempPath(), $"vsa-viewer-{Environment.ProcessId}");
        DotnetRun run = Engine("--export", output);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException($"The rule engine stopped with exit code {run.ExitCode}. {run.Output}");
        }
        return output;
    }

    private static string Metadata(string key)
    {
        return typeof(NotesExport).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == key).Value!;
    }
}
