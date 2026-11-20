namespace Umpire.Vsa.Presenters;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

public interface IPresenterHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent {
    async Task<IMessageResult> IHandler<TEvent>.RunAsync(Envelope<TEvent> envelope) {
        IMessageResult result;
        try {
            result = await HandleAsync(envelope);
        } catch (Exception handlerError) when (!envelope.Cancellation.IsCancellationRequested) {
            try {
                (this as Presenter)?.RaiseChanged();
            } catch (Exception listenerError) {
                throw new AggregateException(handlerError, listenerError);
            }
            throw;
        }
        (this as Presenter)?.RaiseChanged();
        return result;
    }
}
