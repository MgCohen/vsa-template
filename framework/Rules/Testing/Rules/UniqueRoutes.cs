using System.Reflection;
using System.Text.RegularExpressions;
using VSA.Contracts.Requests;

namespace VSA.Testing.Rules;

// VSA2002: two requests do not use the same method and route, in all modules.
// The compare ignores case, a trailing "/" and the names of route parameters: "/notes/{id}" is "/notes/{noteId}".
public sealed partial class UniqueRoutes : IVsaRule {
    public string Id => "VSA2002";
    public string Title => "Two requests do not use the same method and route";

    static readonly MethodInfo ReadMethod = typeof(UniqueRoutes).GetMethod(nameof(Read), BindingFlags.NonPublic | BindingFlags.Static)!;

    public IEnumerable<Violation> Check(VsaApp app) {
        var routes = app.Assemblies.SelectMany(VsaRules.TypesOf)
            .Where(OneHandlerPerRequest.IsRequest)
            .Select(t => (Request: t, Route: ((HttpMethod, string))ReadMethod.MakeGenericMethod(t).Invoke(null, null)!))
            .GroupBy(r => (r.Route.Item1.Method.ToUpperInvariant(), Normalize(r.Route.Item2)));

        foreach (var group in routes.Where(g => g.Count() > 1)) {
            var (method, route) = group.First().Route;
            yield return new(Id, $"{string.Join(", ", group.Select(r => r.Request.FullName))} all use {method} {route}. Give each request its own method and route.");
        }
    }

    static (HttpMethod, string) Read<TRequest>() where TRequest : IRoutedRequest => (TRequest.Method, TRequest.Route);

    static string Normalize(string route) => "/" + Parameter().Replace(route, "{}").Trim('/').ToLowerInvariant();

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex Parameter();
}
