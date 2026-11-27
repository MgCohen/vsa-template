namespace Umpire.Cli.Features.Scaffold.Domain.Setup;

using System.Text.Json;
using System.Text.Json.Nodes;

static class JsonText {
    public static JsonObject? Parse(string text) {
        try {
            return JsonNode.Parse(text) as JsonObject;
        } catch (JsonException) {
            return null;
        }
    }
}
