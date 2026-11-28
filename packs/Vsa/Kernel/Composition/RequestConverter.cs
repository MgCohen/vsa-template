namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

public sealed class RequestConverter : IOperationConverter {
    public const string NotFoundCode = "operation.not_found";
    public const string AmbiguousCode = "operation.ambiguous";

    readonly Dictionary<string, RequestBinder?> byName = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, RequestBinder?> byShortName = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<Type> requests = [];

    RequestConverter(IEnumerable<Type> requests, AppTypes types) {
        foreach (var request in requests) {
            this.requests.Add(request);
            if (request.IsAbstract || request.ContainsGenericParameters) {
                continue;
            }
            var binder = new RequestBinder(request);
            var name = RequestNames.Of(request, types);
            byName[name] = byName.ContainsKey(name) ? null : binder;
            var shortName = RequestNames.NameOf(request);
            byShortName[shortName] = byShortName.ContainsKey(shortName) ? null : binder;
        }
    }

    public IReadOnlyCollection<string> Names => byName.Keys;

    public static RequestConverter Of(AppTypes types, Func<Type, bool>? exposes = null) => new(
        types.Implementations(typeof(RequestHandler<,>))
            .Select(handler => handler.Service.GetGenericArguments()[0])
            .Where(request => exposes is null || exposes(request))
            .Distinct(),
        types);

    public bool Accepts(Type message) {
        for (var type = message; type is not null; type = type.BaseType) {
            if (requests.Contains(type)) {
                return true;
            }
        }
        return false;
    }

    public Result<object> Convert(Operation operation) {
        if (byName.TryGetValue(operation.Name, out var binder) || byShortName.TryGetValue(operation.Name, out binder)) {
            return binder is null
                ? new Error(AmbiguousCode,
                    $"More than one request has the operation name '{operation.Name}'. Give each one its own static Name or Group, or use the full name.")
                : binder.Bind(operation.Values);
        }
        return new Error(NotFoundCode,
            $"The app has no request with the operation name '{operation.Name}'. The names are: {string.Join(", ", Names.OrderBy(n => n, StringComparer.Ordinal))}.");
    }
}

