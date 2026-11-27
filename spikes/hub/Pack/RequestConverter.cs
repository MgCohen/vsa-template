namespace Spike.Pack;

using Spike.Core;

sealed class RequestConverter(IReadOnlyDictionary<string, RequestBinder> binders) : IOperationConverter {
    public MessageResult Convert(Operation operation) =>
        binders.TryGetValue(operation.Name, out var binder)
            ? binder.Bind(operation)
            : MessageResult.Fail($"No request has the name '{operation.Name}'.");
}
