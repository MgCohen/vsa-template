using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireImplementors]
public interface IRunner {
    Task RunAsync(Envelope envelope, Func<Envelope, Task> run);

    Task StartAsync();
}
