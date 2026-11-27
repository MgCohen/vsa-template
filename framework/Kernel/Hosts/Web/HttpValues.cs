namespace Umpire.Hosts.Web;

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

static class HttpValues {
    internal static async Task<IReadOnlyDictionary<string, string?>> Read(HttpRequest request, CancellationToken ct) {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (await Body(request, ct) is { } body) {
            foreach (var (key, node) in body) {
                values[key] = node is null ? null : node is JsonValue value && value.TryGetValue<string>(out var text) ? text : node.ToJsonString();
            }
        }
        foreach (var (key, query) in request.Query) {
            values.TryAdd(key, query.ToString());
        }
        return values;
    }

    internal static bool IsReadError(Exception e) => e is JsonException;

    static async Task<JsonObject?> Body(HttpRequest request, CancellationToken ct) {
        if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method) && !HttpMethods.IsPatch(request.Method)) {
            return null;
        }
        using var reader = new StreamReader(request.Body);
        var text = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        return JsonNode.Parse(text) as JsonObject
            ?? throw new JsonException("The body of the request is not a JSON object.");
    }
}
