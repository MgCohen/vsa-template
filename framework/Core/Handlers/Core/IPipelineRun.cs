using VSA.Contracts;

namespace VSA.Handlers;

interface IPipelineRun {
    Type MessageBase { get; }

    Task<Envelope> RunAsync(object message, IServiceProvider services, CancellationToken ct);
}
