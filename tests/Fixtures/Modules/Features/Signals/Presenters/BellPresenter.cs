namespace Signals.Presenters;

using Signals.Slices.Ring;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

public sealed class BellPresenter(IRequestSender requests) : Presenter(requests), IPresenterEvent<Rang> {
    public string? Last { get; private set; }

    public Task OnEventAsync(Rang integrationEvent, CancellationToken ct) {
        Last = integrationEvent.Bell;
        return Task.CompletedTask;
    }
}
