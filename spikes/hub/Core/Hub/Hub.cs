namespace Spike.Core;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

sealed class Hub(IEnumerable<IHubStep> steps, IEnumerable<IPipeline> pipelines, IServiceScopeFactory scopes) : IHub {
    readonly IHubStep[] hubSteps = [.. steps];
    readonly IPipeline[] all = [.. pipelines];
    readonly ConcurrentDictionary<Type, IPipeline?> chosen = new();

    public async Task<MessageResult> Send(object message, CancellationToken ct = default) {
        var pipeline = chosen.GetOrAdd(message.GetType(), type => all.SingleOrDefault(p => p.Runs(type)));
        if (pipeline is null) {
            return MessageResult.Fail($"No pipeline runs {message.GetType().Name}.");
        }
        using var scope = scopes.CreateScope();
        MessageNext run = () => pipeline.Run(message, scope.ServiceProvider, ct);
        foreach (var step in hubSteps.Reverse()) {
            var next = run;
            run = () => step.Run(message, next);
        }
        try {
            return await run().ConfigureAwait(false);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return MessageResult.Fail($"{message.GetType().Name} failed: {e.Message}");
        }
    }
}
