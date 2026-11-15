using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

interface IPipelineRun {
    bool Runs(Type message);

    Task<Envelope> RunAsync(object message, IServiceProvider services, CancellationToken ct);
}
