namespace Spike.Pack;

using Spike.Core;

sealed class RequestConverter(IReadOnlyDictionary<string, RequestBinder> binders) : IOperationConverter {
    public bool TryConvert(Operation operation, out object message) {
        if (binders.TryGetValue(operation.Name, out var binder)) {
            message = binder.Bind(operation.Values);
            return true;
        }
        message = null!;
        return false;
    }
}
