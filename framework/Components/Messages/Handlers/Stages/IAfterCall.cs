using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireImplementors]
public interface IAfterCall {
    Task CallAsync(Envelope envelope);
}
