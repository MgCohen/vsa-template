using System.Text.Json;
using System.Text.Json.Serialization;
using Viewer.Export.Model;
using VSA.Architecture.Engine;

namespace Viewer.Export;

public static class ArchitectureExport
{
    public const int Version = 2;

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ExportFile Of(AppArchitecture architecture, string name, string root)
    {
        DefinitionExport definition = new(architecture.Definition);
        AppExport app = new(architecture, definition);
        return new ExportFile(Version, definition.Data(), app.Data(name, root));
    }

    public static void Write(ExportFile file, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(file, options));
    }
}
