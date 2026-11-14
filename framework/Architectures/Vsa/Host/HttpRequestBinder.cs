using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace VSA.Host;

static class HttpRequestBinder {
    internal static async Task<TRequest> Bind<TRequest>(HttpContext http, JsonSerializerOptions json) {
        var values = await Body(http) ?? [];
        foreach (var property in typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (values.Any(pair => string.Equals(pair.Key, property.Name, StringComparison.OrdinalIgnoreCase))
                || TextOf(http.Request, property.Name) is not { } text) {
                continue;
            }
            var value = TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(text);
            values[property.Name] = JsonSerializer.SerializeToNode(value, property.PropertyType, json);
        }
        return values.Deserialize<TRequest>(json)
            ?? throw new JsonException($"The request is not a {typeof(TRequest).Name}.");
    }

    internal static bool IsBindError(Exception e) =>
        e is JsonException or FormatException or ArgumentException or NotSupportedException;

    static string? TextOf(HttpRequest request, string name) =>
        request.RouteValues.TryGetValue(name, out var route) && route is not null
            ? Convert.ToString(route, CultureInfo.InvariantCulture)
            : request.Query.TryGetValue(name, out var query) ? query.ToString() : null;

    static async Task<JsonObject?> Body(HttpContext http) {
        if (!HttpMethods.IsPost(http.Request.Method) && !HttpMethods.IsPut(http.Request.Method)
            && !HttpMethods.IsPatch(http.Request.Method)) {
            return null;
        }
        using var reader = new StreamReader(http.Request.Body);
        var text = await reader.ReadToEndAsync(http.RequestAborted);
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        return JsonNode.Parse(text) as JsonObject
            ?? throw new JsonException("The body of the request is not a JSON object.");
    }
}
