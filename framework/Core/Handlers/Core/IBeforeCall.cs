using VSA.Contracts;

namespace VSA.Handlers;

interface IBeforeCall {
    int Order { get; }

    Task<object> CallAsync(Envelope envelope);
}
