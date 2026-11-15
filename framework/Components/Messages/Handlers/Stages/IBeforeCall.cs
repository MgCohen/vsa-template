using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireImplementors]
public interface IBeforeCall {
    Task<object> CallAsync(Envelope envelope);
}
