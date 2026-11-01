using VSA.Contracts.Requests;
using VSA.Handlers.Requests;

namespace VSA.Testing.Architecture.Rules;

// VSA2001: each request has exactly one handler. The handler is the endpoint, so zero handlers means no endpoint,
// and two handlers means two endpoints for one route.
public sealed class OneHandlerPerRequest : IVsaRule {
    public string Id => "VSA2001";
    public string Title => "Each request has exactly one handler";

    public IEnumerable<Violation> Check(VsaApp app) {
        var types = app.Assemblies.SelectMany(VsaRules.TypesOf).ToList();
        var requests = types.Where(IsRequest);
        var handlers = types
            .Where(t => t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            .Select(t => (Handler: t, Base: VsaRules.GenericBase(t, typeof(RoutedHandler<,>))))
            .Where(h => h.Base is not null)
            .ToLookup(h => h.Base!.GetGenericArguments()[0], h => h.Handler);

        foreach (var request in requests) {
            var found = handlers[request].ToList();
            if (found.Count == 0) {
                yield return new(Id, $"{request.FullName} has no handler. Add one class that inherits RoutedHandler<{request.Name}, TResponse> in the Handlers/Requests project of the module.");
            } else if (found.Count > 1) {
                yield return new(Id, $"{request.FullName} has {found.Count} handlers: {string.Join(", ", found.Select(h => h.FullName))}. Keep one handler and remove the others.");
            }
        }
    }

    internal static bool IsRequest(Type type) =>
        type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false } && typeof(IRoutedRequest).IsAssignableFrom(type);
}
