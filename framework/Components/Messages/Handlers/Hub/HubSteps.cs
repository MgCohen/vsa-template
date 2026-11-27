namespace Umpire.Messages.Handlers;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

sealed class HubSteps(IEnumerable<HubStepEntry> entries) {
    readonly Type[] steps = [.. entries.Select(entry => entry.Step)];

    internal async Task<TResult> RunAsync<TResult>(
        object message, IServiceProvider services, Func<Task<TResult>> run, Func<Error, TResult> rejected)
        where TResult : IMessageResult {
        var entered = new List<HubStep>();
        IMessageResult? outcome = null;
        try {
            foreach (var type in steps) {
                var step = (HubStep)ActivatorUtilities.GetServiceOrCreateInstance(services, type);
                if (await step.BeforeAsync(message, services) is { } error) {
                    var refused = rejected(error);
                    outcome = refused;
                    return refused;
                }
                entered.Add(step);
            }
            var result = await run();
            outcome = result;
            return result;
        } finally {
            for (var i = entered.Count - 1; i >= 0; i--) {
                await entered[i].AfterAsync(message, outcome, services);
            }
        }
    }
}
