namespace SampleApp.Features.SampleModule.Presenters;

using SampleApp.Features.SampleModule.Slices.GetStatus;
using VSA.Presenters;

public sealed class SampleModulePresenter(IBus bus) : Presenter(bus) {
    public string Status { get; private set; } = "";
    public OpState Load { get; } = new();

    public Task LoadAsync() => Run(Load, async () => {
        var status = await Bus.Send(new GetStatusRequest());
        if (status.IsSuccess) {
            Status = status.Value.Status;
        }
        return status.Error;
    });
}
