using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Contracts.Commands;

namespace VSA.Handlers.Commands;

sealed class InProcessCommandSender(IServiceProvider provider) : ICommandSender {
    public Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct) =>
        provider.GetRequiredService<Pipelines>().SendAsync<TResult>(PipelineKind.Commands, command, provider, ct);
}
