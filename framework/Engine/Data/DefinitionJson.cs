using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
#if NET
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
#endif
using System.Text.Json.Serialization;

namespace VSA.Architecture.Data;

public static class DefinitionJson {
    public const int Version = 2;

    public const string SchemaName = "architecture-definition.v2.json";

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
#if NET
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
#endif
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

    public static string Hash(DefinitionFile file) {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(Write(file)));
        return "sha256:" + BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    public static void Save(DefinitionFile file, string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Write(file));
    }

#if NET
    public static JsonNode Schema() => Options.GetJsonSchemaAsNode(typeof(DefinitionFile));
#endif
}
