namespace SampleApp.Features.SampleModule.Presenters;

using SampleApp.Features.SampleModule.Slices.GetStatus;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

public sealed class SampleModulePresenter(IRequestSender requests) : Presenter(requests) {
    public string Status { get; private set; } = "";
    public OpState Load { get; } = new();

    public Task LoadAsync() => Run(Load, async () => {
        var status = await Requests.SendAsync(new GetStatusRequest());
        if (status.IsSuccess) {
            Status = status.Value.Status;
        }
        return status.Error;
    });
}
