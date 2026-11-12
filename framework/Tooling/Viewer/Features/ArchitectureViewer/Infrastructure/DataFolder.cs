namespace ArchitectureViewer.Infrastructure;

public static class DataFolder
{
    public const string Name = "Data";

    public static ArchitectureFiles Read(string appFolder)
    {
        string data = Path.Combine(appFolder, Name);
        string layout = File.ReadAllText(Path.Combine(data, "layout.json"));
        return new ArchitectureFiles(layout, File.ReadAllText(Path.Combine(data, "architecture.json")));
    }
}
