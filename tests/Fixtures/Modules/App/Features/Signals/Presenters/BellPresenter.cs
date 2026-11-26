namespace Signals.Presenters;

using Signals.Slices.Ring;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Presenters;

public sealed class BellPresenter(IRequestSender requests) : Presenter(requests), IIntegrationEventHandler<Rang> {
    public string? Last { get; private set; }

    public Task<Result<Unit>> HandleAsync(IEnvelope<Rang> envelope) {
        Last = envelope.Message.Bell;
        return Task.FromResult<Result<Unit>>(Unit.Value);
    }
}
