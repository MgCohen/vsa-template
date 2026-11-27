namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public sealed class Pipeline : IPipeline {
    readonly Dictionary<Type, MessageRoute> routes;

    public Pipeline(IReadOnlyDictionary<Type, HandlerRoute> handlers, IEnumerable<StepEntry> steps, IEnumerable<ValidatorEntry> validators) {
        StepEntry[] allSteps = [.. steps];
        ValidatorEntry[] allValidators = [.. validators];
        routes = handlers.ToDictionary(h => h.Key, h => new MessageRoute(
            h.Value,
            [.. allSteps.Where(s => s.Message.IsAssignableFrom(h.Key)).Select(s => s.Step)],
            [.. allValidators.Where(v => v.Message.IsAssignableFrom(h.Key)).Select(v => v.Validator)]));
    }

    public IReadOnlyCollection<Type> Messages => routes.Keys;

    public async Task<MessageResult> Run(object message, IServiceProvider scope, CancellationToken ct) {
        var route = routes[message.GetType()];
        MessageNext run = async () => {
            foreach (var type in route.Validators) {
                if (((IValidator)scope.GetRequiredService(type)).Check(message) is { } error) {
                    return MessageResult.Fail(error);
                }
            }
            var handler = scope.GetRequiredService(route.HandlerRoute.Service);
            var value = await route.HandlerRoute.HandlerCall.Call(handler, message, ct).ConfigureAwait(false);
            return value as MessageResult ?? MessageResult.Ok(value);
        };
        for (var i = route.Steps.Count - 1; i >= 0; i--) {
            var step = (IPipelineStep)scope.GetRequiredService(route.Steps[i]);
            var next = run;
            run = () => step.Run(message, next);
        }
        return await run().ConfigureAwait(false);
    }
}
