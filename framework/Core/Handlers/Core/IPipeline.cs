using VSA.Contracts;

namespace VSA.Handlers;

public interface IPipeline<TMessage> where TMessage : class {
    IRunner Runner { get; }

    Task<Result<TResult>> SendAsync<TResult>(TMessage message, CancellationToken ct = default);

    Task StartAsync();
}
