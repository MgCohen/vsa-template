namespace Umpire.Messages.Handlers;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

static class PipelineRun {
    internal static async Task ExecuteAsync(MessageRoute route, Envelope envelope) {
        CurrentEnvelope.Value = envelope;
        envelope.State = EnvelopeState.Running;
        var fault = new FirstFault(envelope);
        try {
            await UntilHandledAsync(route, envelope);
        } catch (Exception e) {
            fault.Add(e);
        }
        envelope.Stage = EnvelopeStage.AfterHooks;
        await EachAfterAsync(route.AfterHooks, envelope, fault);
        envelope.Stage = EnvelopeStage.AfterSteps;
        await EachAfterAsync(route.AfterSteps, envelope, fault);
        envelope.Stage = EnvelopeStage.Done;
        fault.ThrowIfAny();
    }

    static async Task UntilHandledAsync(MessageRoute route, Envelope envelope) {
        envelope.Stage = EnvelopeStage.BeforeSteps;
        if (!await BeforeAsync(Instances<IBeforeCall>(route.BeforeValidationSteps, envelope), envelope)) {
            return;
        }
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
            if (route.Handlers.Count == 0 && route.CloseFailures.Count > 0) {
                throw new InvalidOperationException(
                    $"{route.Message.FullName} has no handler in its pipeline. "
                    + string.Join(" ", route.CloseFailures)
                    + " Change the message, or change the constraints of the open handler.");
            }
            if (route.Handlers.Count == 0) {
                throw new InvalidOperationException(
                    $"{route.Message.FullName} has no handler in its pipeline. Add one handler for it: a class that "
                    + $"implements IHandler<{route.Message.Name}>, directly or through a handler base.");
            }
            var handler = route.Handlers[0];
            return await route.CallAsync(handler, Resolve<IHandler>(handler.Handler, envelope), envelope);
        }
        IMessageResult result = (Result<Unit>)Unit.Value;
        foreach (var handler in route.Handlers) {
            var handled = await OneOfManyAsync(route, handler, envelope);
            if (!handled.IsSuccess && result.IsSuccess) {
                result = handled;
            }
        }
        return result;
    }

    static async Task<IMessageResult> OneOfManyAsync(MessageRoute route, RouteHandler handler, Envelope envelope) {
        try {
            return await route.CallAsync(handler, Resolve<IHandler>(handler.Handler, envelope), envelope);
        } catch (Exception e)
            when (e is not OperationCanceledException || !envelope.Cancellation.IsCancellationRequested) {
            envelope.AddHandlerException(e);
            return (Result<Unit>)new Error(EnvelopeResults.FaultCode, $"{handler.Handler.Name} failed: {e.Message}");
        }
    }

    static async Task EachAfterAsync(IReadOnlyList<Type> calls, Envelope envelope, FirstFault fault) {
        foreach (var call in calls) {
            try {
                await Resolve<IAfterCall>(call, envelope).CallAsync(envelope);
            } catch (Exception e) {
                fault.Add(e);
            }
        }
    }

    static List<T> Instances<T>(IReadOnlyList<Type> types, Envelope envelope) =>
        [.. types.Select(type => Resolve<T>(type, envelope))];

    static T Resolve<T>(Type type, Envelope envelope) =>
        (T)ActivatorUtilities.GetServiceOrCreateInstance(envelope.Services, type);
}
