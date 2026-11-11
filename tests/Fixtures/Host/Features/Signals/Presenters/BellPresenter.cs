using Signals.Slices.Ring;
using VSA.Presenters;

namespace Signals.Presenters;

// Shows the last bell. It gets Rang from the Contracts.Events project of its module.
public sealed class BellPresenter(IBus bus) : Presenter(bus), IPresenterEvent<Rang> {
    public string? Last { get; private set; }

    public Task OnEventAsync(Rang integrationEvent, CancellationToken ct) {
        Last = integrationEvent.Bell;
        return Task.CompletedTask;
    }
}
