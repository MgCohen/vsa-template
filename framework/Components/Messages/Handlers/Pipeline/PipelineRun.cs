namespace Umpire.Messages.Handlers;

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

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
        foreach (var validator in Instances<IValidation>(route.Validators, envelope)) {
            if (validator.Validate(envelope) is { } error) {
                envelope.Reject(error);
                return;
            }
        }
        envelope.Stage = EnvelopeStage.BeforeSteps;
        if (!await BeforeAsync(Instances<IBeforeCall>(route.BeforeSteps, envelope), envelope)) {
            return;
        }
        envelope.Stage = EnvelopeStage.BeforeHooks;
        if (!await BeforeAsync(Instances<IBeforeCall>(route.BeforeHooks, envelope), envelope)) {
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
                    $"{route.Message.FullName} has no handler in its pipeline. Add one handler for it: a class that "
                    + $"implements IHandler<{route.Message.Name}>, directly or through a handler base.");
            }
            return await route.Call(Resolve<IHandler>(route.Handlers[0], envelope), envelope);
        }
        IMessageResult result = (Result<Unit>)Unit.Value;
        foreach (var type in route.Handlers) {
            var handled = await OneOfManyAsync(route, type, envelope);
            if (!handled.IsSuccess && result.IsSuccess) {
                result = handled;
            }
        }
        return result;
    }

    static async Task<IMessageResult> OneOfManyAsync(MessageRoute route, Type handler, Envelope envelope) {
        try {
            return await route.Call(Resolve<IHandler>(handler, envelope), envelope);
        } catch (Exception e)
            when (e is not OperationCanceledException || !envelope.Cancellation.IsCancellationRequested) {
            envelope.AddHandlerException(e);
            return (Result<Unit>)new Error(EnvelopeResults.FaultCode, $"{handler.Name} failed: {e.Message}");
        }
    }

    static async Task AfterAsync(MessageRoute route, Envelope envelope) {
        envelope.Stage = EnvelopeStage.AfterHooks;
        foreach (var call in Instances<IAfterCall>(route.AfterHooks, envelope)) {
            await call.CallAsync(envelope);
        }
        envelope.Stage = EnvelopeStage.AfterSteps;
        foreach (var call in Instances<IAfterCall>(route.AfterSteps, envelope)) {
            await call.CallAsync(envelope);
        }
    }

    static List<T> Instances<T>(IReadOnlyList<Type> types, Envelope envelope) =>
        [.. types.Select(type => Resolve<T>(type, envelope))];

    static T Resolve<T>(Type type, Envelope envelope) =>
        (T)ActivatorUtilities.GetServiceOrCreateInstance(envelope.Services, type);
}
