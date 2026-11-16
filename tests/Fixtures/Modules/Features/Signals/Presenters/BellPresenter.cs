namespace Signals.Presenters;

using Signals.Slices.Ring;
using Umpire.Vsa.Presenters;

public sealed class BellPresenter(IBus bus) : Presenter(bus), IPresenterEvent<Rang> {
    public string? Last { get; private set; }

    public Task OnEventAsync(Rang integrationEvent, CancellationToken ct) {
        Last = integrationEvent.Bell;
        return Task.CompletedTask;
    }
}
