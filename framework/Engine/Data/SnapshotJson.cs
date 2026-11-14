using System.Text.Json;
#if NET
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
#endif

namespace VSA.Architecture.Data;

public static class SnapshotJson {
    public const int Version = 1;

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
#if NET
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
#endif
    };

    public static SnapshotFile Read(string json) {
        var file = JsonSerializer.Deserialize<SnapshotFile>(json, Options)
            ?? throw new JsonException("The file has no snapshot.");
        if (file.Version != Version) {
            throw new JsonException(
                $"The file has version {file.Version}, and the reader reads version {Version}. Export it again.");
        }
        return file;
    }

    public static string Write(SnapshotFile file) => JsonSerializer.Serialize(file, Options);

    public static void Save(SnapshotFile file, string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Write(file));
    }

#if NET
    public static JsonNode Schema() => Options.GetJsonSchemaAsNode(typeof(SnapshotFile));
#endif
}
