namespace Umpire.Cli.Scaffolding.Setup;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

static class JsonText {
    static readonly JsonSerializerOptions Indented = new() {
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static readonly Regex FirstIndent = new(@"\n( +|\t+)""");

    public static JsonObject? Parse(string text) {
        try {
            return JsonNode.Parse(text) as JsonObject;
        } catch (JsonException) {
            return null;
        }
    }

    public static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static string Write(JsonObject json, string? indentOf = null) {
        var options = Indented;
        if (indentOf is not null && FirstIndent.Match(indentOf) is { Success: true } indent) {
            var chars = indent.Groups[1].Value;
            options = new JsonSerializerOptions(Indented) { IndentCharacter = chars[0], IndentSize = chars.Length };
        }
        return json.ToJsonString(options) + "\n";
    }
}
