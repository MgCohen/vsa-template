namespace Umpire.Messages.Handlers;

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

sealed class HubSteps(IEnumerable<HubStepEntry> entries) {
    readonly Type[] steps = [.. entries
        .Select((entry, index) => new { entry, index })
        .OrderBy(e => e.entry.Priority)
        .ThenBy(e => e.index)
        .Select(e => e.entry.Step)];

    internal async Task<TResult> RunAsync<TResult>(
        object message, IServiceProvider services, CancellationToken ct, Func<Task<TResult>> run, Func<Error, TResult> rejected)
        where TResult : IMessageResult {
        var entered = new List<HubStep>();
        IMessageResult? outcome = null;
        ExceptionDispatchInfo? first = null;
        TResult result = default!;
        try {
            result = await BeforeAndRunAsync(message, services, ct, run, rejected, entered);
            outcome = result;
        } catch (Exception e) {
            first = ExceptionDispatchInfo.Capture(e);
        }
        for (var i = entered.Count - 1; i >= 0; i--) {
            try {
                await entered[i].AfterAsync(message, outcome, services, ct);
            } catch (Exception e) {
                first ??= ExceptionDispatchInfo.Capture(e);
            }
        }
        first?.Throw();
        return result;
    }

    async Task<TResult> BeforeAndRunAsync<TResult>(
        object message, IServiceProvider services, CancellationToken ct, Func<Task<TResult>> run, Func<Error, TResult> rejected,
        List<HubStep> entered) {
        foreach (var type in steps) {
            var step = (HubStep)ActivatorUtilities.GetServiceOrCreateInstance(services, type);
            if (await step.BeforeAsync(message, services, ct) is { } error) {
                return rejected(error);
            }
            entered.Add(step);
        }
        return await run();
    }
}
