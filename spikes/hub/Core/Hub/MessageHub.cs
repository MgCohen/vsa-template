namespace Spike.Core;

public sealed class MessageHub {
    readonly IHubStep[] reversedSteps;
    readonly Dictionary<Type, IPipeline> byMessage = [];

    public MessageHub(IEnumerable<IHubStep> steps, IEnumerable<IPipeline> pipelines) {
        reversedSteps = [.. steps.Reverse()];
        foreach (var pipeline in pipelines) {
            foreach (var message in pipeline.Messages) {
                if (byMessage.ContainsKey(message)) {
                    throw new InvalidOperationException($"Two pipelines run {message.Name}.");
                }
                byMessage[message] = pipeline;
            }
        }
    }

    internal async Task<MessageResult> Send(object message, IServiceProvider scope, CancellationToken ct) {
        if (!byMessage.TryGetValue(message.GetType(), out var pipeline)) {
            return MessageResult.Fail($"No pipeline runs {message.GetType().Name}.");
        }
        MessageNext run = () => Guarded(pipeline, message, scope, ct);
        foreach (var step in reversedSteps) {
            var next = run;
            run = () => step.Run(message, next);
        }
        return await run().ConfigureAwait(false);
    }

    static async Task<MessageResult> Guarded(IPipeline pipeline, object message, IServiceProvider scope, CancellationToken ct) {
        try {
            return await pipeline.Run(message, scope, ct).ConfigureAwait(false);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return MessageResult.Fail($"{message.GetType().Name} failed: {e.Message}");
        }
    }
}
