namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

sealed class Hub : IHub {
    readonly IHubStep[] hubSteps;
    readonly Dictionary<Type, IPipeline> byMessage = [];
    readonly IServiceScopeFactory scopes;

    public Hub(IEnumerable<IHubStep> steps, IEnumerable<IPipeline> pipelines, IServiceScopeFactory scopes) {
        hubSteps = [.. steps];
        this.scopes = scopes;
        foreach (var pipeline in pipelines) {
            foreach (var message in pipeline.Messages) {
                if (byMessage.ContainsKey(message)) {
                    throw new InvalidOperationException($"Two pipelines run {message.Name}.");
                }
                byMessage[message] = pipeline;
            }
        }
    }

    public async Task<MessageResult> Send(object message, CancellationToken ct = default) {
        if (!byMessage.TryGetValue(message.GetType(), out var pipeline)) {
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
