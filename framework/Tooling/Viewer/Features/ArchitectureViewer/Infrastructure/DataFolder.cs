namespace ArchitectureViewer.Infrastructure;

public static class DataFolder
{
    public const string Name = "Data";

    public const string InputKey = "Viewer:Architecture";

    public static ArchitectureFiles Read(string appFolder, string? input)
    {
        string path = input ?? Path.Combine(appFolder, Name, "architecture.json");
        return new ArchitectureFiles(File.ReadAllText(path));
    }
}
