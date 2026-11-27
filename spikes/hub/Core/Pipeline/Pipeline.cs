namespace Spike.Core;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

public sealed class Pipeline(Type messageBase, IReadOnlyDictionary<Type, HandlerRoute> handlers) : IPipeline {
    readonly ConcurrentDictionary<Type, MessageRoute> routes = new();

    public bool Runs(Type message) => messageBase.IsAssignableFrom(message) && handlers.ContainsKey(message);

    public async Task<MessageResult> Run(object message, IServiceProvider scope, CancellationToken ct) {
        var route = routes.GetOrAdd(message.GetType(), type => MessageRoute.For(type, handlers[type], scope));
        MessageNext run = async () => {
            foreach (var validator in route.Validators(scope)) {
                if (validator.Check(message) is { } error) {
                    return MessageResult.Fail(error);
                }
            }
            var handler = scope.GetRequiredService(route.Handler.Service);
            return MessageResult.Ok(await route.Handler.Call.Call(handler, message, ct).ConfigureAwait(false));
        };
        foreach (var step in route.Steps(scope).Reverse()) {
            var next = run;
            run = () => step.Run(message, next);
        }
        return await run().ConfigureAwait(false);
    }
}
