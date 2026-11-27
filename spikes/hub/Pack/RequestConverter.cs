namespace Spike.Pack;

using Spike.Core;

sealed class RequestConverter : IOperationConverter {
    readonly Dictionary<string, RequestBinder> binders = new(StringComparer.OrdinalIgnoreCase);

    public RequestConverter(IReadOnlyDictionary<string, Type> requests, IConstructorFactory factory) {
        foreach (var request in requests) {
            binders[request.Key] = new RequestBinder(request.Value, factory);
        }
    }

    public MessageResult Convert(Operation operation) =>
        binders.TryGetValue(operation.Name, out var binder)
            ? binder.Bind(operation)
            : MessageResult.Fail($"No request has the name '{operation.Name}'.");
}
