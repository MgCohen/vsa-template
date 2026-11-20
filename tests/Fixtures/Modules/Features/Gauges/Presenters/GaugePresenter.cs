namespace Gauges.Presenters;

using Gauges.Domain.Shared;
using Gauges.Slices.ReadGauge;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

public sealed class GaugePresenter<T>(IRequestSender requests) : Presenter(requests), IPresenterHandler<GaugeRead>
    where T : struct, IGauge {
    public int Last { get; private set; }

    public Task<Result<Unit>> HandleAsync(Envelope<GaugeRead> envelope) {
        Last = envelope.Message.Value;
        return Task.FromResult<Result<Unit>>(Unit.Value);
    }
}
