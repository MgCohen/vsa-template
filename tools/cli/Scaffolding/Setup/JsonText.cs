namespace Umpire.Cli.Scaffolding.Setup;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

static class JsonText {
    static readonly JsonSerializerOptions Indented = new() {
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static JsonObject? Parse(string text) {
        try {
            return JsonNode.Parse(text) as JsonObject;
        } catch (JsonException) {
            return null;
        }
    }

    public static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static string Write(JsonObject json) => json.ToJsonString(Indented) + "\n";
}
