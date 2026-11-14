using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireImplementors]
interface IBeforeCall {
    int Order { get; }

    Task<object> CallAsync(Envelope envelope);
}
