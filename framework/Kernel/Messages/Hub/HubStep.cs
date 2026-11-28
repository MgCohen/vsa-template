namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public abstract class HubStep {
    public virtual Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) =>
        Task.FromResult<Error?>(null);

    public virtual Task AfterAsync(object message, IMessageResult? result, IServiceProvider services, CancellationToken ct) =>
        Task.CompletedTask;
}
