using VSA.Contracts;

namespace VSA.Handlers;

interface IAfterCall {
    int Order { get; }

    Task CallAsync(Envelope envelope);
}
