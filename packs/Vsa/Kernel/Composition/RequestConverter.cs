namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

public sealed class RequestConverter : IOperationConverter {
    public const string NotFoundCode = "operation.not_found";

    readonly Dictionary<string, RequestBinder> byName = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, RequestBinder?> byShortName = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<Type> requests = [];

    RequestConverter(IEnumerable<Type> requests, AppTypes types) {
        foreach (var request in requests) {
            var name = RequestNames.Of(request, types);
            if (byName.ContainsKey(name)) {
                throw new InvalidOperationException(
                    $"Two requests have the operation name '{name}'. Give one of them its own static Name or Group.");
            }
            var binder = new RequestBinder(request);
            byName[name] = binder;
            var shortName = RequestNames.NameOf(request);
            byShortName[shortName] = byShortName.ContainsKey(shortName) ? null : binder;
            this.requests.Add(request);
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
        if (byName.TryGetValue(operation.Name, out var binder)
            || (byShortName.TryGetValue(operation.Name, out binder) && binder is not null)) {
            return binder.Bind(operation.Values);
        }
        return new Error(NotFoundCode,
            $"The app has no request with the operation name '{operation.Name}'. The names are: {string.Join(", ", Names.OrderBy(n => n, StringComparer.Ordinal))}.");
    }
}

