using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;

namespace VSA.Handlers;

static class PipelineRun {
    internal static async Task ExecuteAsync(MessageRoute route, Envelope envelope) {
        CurrentEnvelope.Value = envelope;
        envelope.State = EnvelopeState.Running;
        ExceptionDispatchInfo? fault = null;
        try {
            await UntilHandledAsync(route, envelope);
        } catch (Exception e) {
            envelope.Fault(e);
            fault = ExceptionDispatchInfo.Capture(e);
        }
        try {
            await AfterAsync(route, envelope);
        } catch (Exception e) when (fault is null) {
            envelope.Fault(e);
            fault = ExceptionDispatchInfo.Capture(e);
        } catch (Exception) when (fault is not null) {
        }
        envelope.Stage = EnvelopeStage.Done;
        fault?.Throw();
    }

    static async Task UntilHandledAsync(MessageRoute route, Envelope envelope) {
        envelope.Stage = EnvelopeStage.Validate;
        foreach (var validator in Ordered<IValidation>(route.Validators, envelope, v => v.Order)) {
            if (validator.Validate(envelope) is { } error) {
                envelope.Reject(error);
                return;
            }
        }
        envelope.Stage = EnvelopeStage.BeforeSteps;
        if (!await BeforeAsync(Instances<IBeforeCall>(route.BeforeSteps, envelope), envelope)) {
            return;
        }
        envelope.Stage = EnvelopeStage.BeforeEvents;
        if (!await BeforeAsync(Ordered<IBeforeCall>(route.BeforeEvents, envelope, b => b.Order), envelope)) {
            return;
        }
        envelope.Stage = EnvelopeStage.Handle;
        envelope.Accept(await HandleAsync(route, envelope));
    }

    static async Task<bool> BeforeAsync(IEnumerable<IBeforeCall> calls, Envelope envelope) {
        foreach (var call in calls) {
            var message = await call.CallAsync(envelope);
            if (envelope.State == EnvelopeState.Rejected) {
                return false;
            }
            envelope.Replace(message);
        }
        return true;
    }

    static async Task<IMessageResult> HandleAsync(MessageRoute route, Envelope envelope) {
        if (!route.Many) {
            if (route.Handlers.Count == 0) {
                throw new InvalidOperationException(
                    $"{route.Message.FullName} has no handler in its pipeline. Add one handler for it, "
                    + $"for example a CommandHandler<{route.Message.Name}, TResult> in the Handlers.Commands project "
                    + "of its module, or a RequestHandler in its Handlers.Requests project.");
            }
            return await Resolve<IHandler>(route.Handlers[0], envelope).HandleAsync(envelope);
        }
        IMessageResult result = (Result<Unit>)Unit.Value;
        foreach (var type in route.Handlers) {
            var handled = await Resolve<IHandler>(type, envelope).HandleAsync(envelope);
            if (!handled.IsSuccess && result.IsSuccess) {
                result = handled;
            }
        }
        return result;
    }

    static async Task AfterAsync(MessageRoute route, Envelope envelope) {
        envelope.Stage = EnvelopeStage.AfterEvents;
        foreach (var call in Ordered<IAfterCall>(route.AfterEvents, envelope, a => a.Order)) {
            await call.CallAsync(envelope);
        }
        envelope.Stage = EnvelopeStage.AfterSteps;
        foreach (var call in Instances<IAfterCall>(route.AfterSteps, envelope)) {
            await call.CallAsync(envelope);
        }
    }

    static IEnumerable<T> Ordered<T>(IReadOnlyList<Type> types, Envelope envelope, Func<T, int> order) =>
        Instances<T>(types, envelope).OrderBy(order).ThenBy(i => i!.GetType().FullName, StringComparer.Ordinal);

    static List<T> Instances<T>(IReadOnlyList<Type> types, Envelope envelope) =>
        [.. types.Select(type => Resolve<T>(type, envelope))];

    static T Resolve<T>(Type type, Envelope envelope) =>
        (T)ActivatorUtilities.GetServiceOrCreateInstance(envelope.Services, type);
}
