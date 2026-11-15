namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

interface IPipelineRun {
    bool Runs(Type message);

    Task<Envelope> RunAsync(object message, IServiceProvider services, CancellationToken ct);
}
