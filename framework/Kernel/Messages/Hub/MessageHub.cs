namespace Umpire.Messages.Handlers;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

sealed class MessageHub(IServiceProvider services) : IMessageHub {
    public Task<IMessageResult> SendAsync(object message, CancellationToken ct = default) =>
        services.GetRequiredService<Pipelines>().SendAsync(message, services, ct);
}
