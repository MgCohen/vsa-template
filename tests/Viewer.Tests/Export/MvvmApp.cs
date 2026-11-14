using Tests.Shared;

namespace Viewer.Tests.Export;

public static class MvvmApp
{
    private static readonly Lazy<string> folder = new(Make);

    public static string Folder => folder.Value;

    public static string DefinitionJson => File.ReadAllText(Path.Combine(Folder, "definition.json"));

    public static string ArchitectureJson => File.ReadAllText(Path.Combine(Folder, "architecture.json"));

    public static DotnetRun Engine(params string[] arguments)
    {
        return EngineProcess.Run(EngineProcess.Metadata("MvvmRoot"), EngineProcess.Metadata("MvvmOutput"), arguments);
    }

    public static DotnetRun Check(string definitionJson)
    {
        string file = Path.Combine(TempFolder.Create(), "definition.json");
        File.WriteAllText(file, definitionJson);
        return Engine("--definition", file);
    }

    public static IReadOnlyList<string> Errors(DotnetRun run)
    {
        return [.. run.Output.Split('\n').Select(l => l.Trim()).Where(l => l.Contains(": error ", StringComparison.Ordinal))];
    }

    private static string Make()
    {
        string output = TempFolder.Create();
        DotnetRun run = Engine("--export", output);
        if (!run.Output.Contains("VSA3005", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The rule engine did not export the MVVM app. {run.Output}");
        }
        return output;
    }
}
