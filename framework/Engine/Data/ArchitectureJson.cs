using System.Text.Json;
#if NET
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
#endif

namespace VSA.Architecture.Data;

public static class ArchitectureJson {
    public const int Version = 4;

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
#if NET
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
#endif
    };

    public static ArchitectureFile Read(string json) {
        var file = JsonSerializer.Deserialize<ArchitectureFile>(json, Options)
            ?? throw new JsonException("The file has no architecture.");
        if (file.Version != Version) {
            throw new JsonException(
                $"The file has version {file.Version}, and the reader reads version {Version}. Export it again.");
        }
        return file;
    }

    public static string Write(ArchitectureFile file) => JsonSerializer.Serialize(file, Options);

    public static void Save(ArchitectureFile file, string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Write(file));
    }

#if NET
    public static JsonNode Schema() => Options.GetJsonSchemaAsNode(typeof(ArchitectureFile));
#endif
}
