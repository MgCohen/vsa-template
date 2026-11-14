namespace ArchitectureViewer.Infrastructure;

public static class DataFolder
{
    public const string Name = "Data";

    public const string InputKey = "Viewer:Architecture";

    public const string DefinitionFile = "definition.json";

    public static ArchitectureFiles Read(string appFolder, string? input)
    {
        string path = input ?? Path.Combine(appFolder, Name, "architecture.json");
        string definition = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, DefinitionFile);
        return new ArchitectureFiles(File.ReadAllText(path), File.ReadAllText(definition));
    }
}
