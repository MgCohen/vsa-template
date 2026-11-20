namespace Umpire.Vsa.Presenters;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

public interface IPresenterHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent {
    async Task<IMessageResult> IHandler<TEvent>.RunAsync(Envelope<TEvent> envelope) {
        try {
            return await HandleAsync(envelope);
        } finally {
            (this as Presenter)?.RaiseChanged();
        }
    }
}
