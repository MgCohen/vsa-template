using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using VSA.Contracts.Requests;
using VSA.Presenters;

namespace VSA.Host;

// Sends each request over HTTP to the method and route of its request type.
// The host gives the base address. In the host process, it is the host itself (loopback).
sealed class HttpBus(HttpClient http) : IBus {
    static readonly ConcurrentDictionary<Type, (HttpMethod Method, string Route)> routes = new();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) where TResponse : IResponse {
        var type = request.GetType();
        var (method, route) = routes.GetOrAdd(type, RouteOf);
        var bodyless = method == HttpMethod.Get || method == HttpMethod.Delete;
        using var message = new HttpRequestMessage(method, Address(route, request, bodyless));
        if (!bodyless) {
            message.Content = JsonContent.Create(request, type);
        }
        using var response = await http.SendAsync(message, ct);
        if (response.IsSuccessStatusCode) {
            return (await response.Content.ReadFromJsonAsync<TResponse>(ct))!;
        }
        var problem = await ProblemOf(response, ct);
        throw new RequestException(
            problem?.Title ?? $"http.{(int)response.StatusCode}",
            problem?.Detail ?? response.ReasonPhrase ?? "The request failed.");
    }

    static (HttpMethod Method, string Route) RouteOf(Type type) {
        T Read<T>(string name) => (T)type.GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        return (Read<HttpMethod>(nameof(IRoutedRequest.Method)), Read<string>(nameof(IRoutedRequest.Route)));
    }

    // Puts properties into the {placeholders} of the route. A request with no body sends the others in the query.
    static string Address(string route, object request, bool bodyless) {
        var query = new List<string>();
        foreach (var property in request.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var value = Uri.EscapeDataString(Convert.ToString(property.GetValue(request), System.Globalization.CultureInfo.InvariantCulture) ?? "");
            var placeholder = "{" + property.Name + "}";
            if (route.Contains(placeholder, StringComparison.OrdinalIgnoreCase)) {
                route = route.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
            } else if (bodyless) {
                query.Add($"{Uri.EscapeDataString(property.Name)}={value}");
            }
        }
        return query.Count == 0 ? route : $"{route}?{string.Join('&', query)}";
    }

    static async Task<Problem?> ProblemOf(HttpResponseMessage response, CancellationToken ct) {
        try {
            return await response.Content.ReadFromJsonAsync<Problem>(ct);
        } catch (JsonException) {
            return null;
        }
    }

    // The fields of an RFC 9457 problem response that the bus uses.
    sealed record Problem(string? Title, string? Detail);
}
