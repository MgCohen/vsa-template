using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace VSA.Architecture.Data;

public static class DefinitionJson {
    public const int Version = 1;

    public const string SchemaName = "architecture-definition.v1.json";

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static DefinitionFile Read(string json) {
        var file = JsonSerializer.Deserialize<DefinitionFile>(json, Options)
            ?? throw new JsonException("The file has no definition.");
        if (file.Version != Version) {
            throw new JsonException(
                $"The definition file has version {file.Version}, and the reader reads version {Version}. "
                + "Export the definition again.");
        }
        return file;
    }

    public static string Write(DefinitionFile file) => JsonSerializer.Serialize(file, Options);

    public static void Save(DefinitionFile file, string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Write(file));
    }

    public static JsonNode Schema() => Options.GetJsonSchemaAsNode(typeof(DefinitionFile));
}
