using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireImplementors]
interface IAfterCall {
    int Order { get; }

    Task CallAsync(Envelope envelope);
}
