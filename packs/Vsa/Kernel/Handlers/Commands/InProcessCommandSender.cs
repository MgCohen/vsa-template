using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Messages.Handlers;

namespace Umpire.Vsa.Handlers.Commands;

sealed class InProcessCommandSender(IServiceProvider provider) : ICommandSender {
    public Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct) =>
        provider.GetRequiredService<Pipelines>().SendAsync<TResult>(typeof(CommandHandler<,>), command, provider, ct);
}
