using VSA.Contracts;

namespace VSA.Handlers;

interface IPipelineRun {
    bool Runs(Type message);

    Task<Envelope> RunAsync(object message, IServiceProvider services, CancellationToken ct);
}
