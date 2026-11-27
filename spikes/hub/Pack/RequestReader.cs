namespace Spike.Pack;

using Spike.Core;

sealed class RequestReader(IReadOnlyDictionary<string, RequestBinder> binders) : IOperationReader {
    public bool TryRead(Operation operation, out object message) {
        if (operation.Message is IRequest typed) {
            message = typed;
            return true;
        }
        if (binders.TryGetValue(operation.Name, out var binder)) {
            message = binder.Bind(operation.Values);
            return true;
        }
        message = null!;
        return false;
    }
}
