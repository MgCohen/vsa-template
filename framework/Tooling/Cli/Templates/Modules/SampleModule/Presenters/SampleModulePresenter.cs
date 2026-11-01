using SampleModule.Contracts.Requests.GetStatus;
using VSA.Presenters;

namespace SampleModule.Presenters;

public sealed class SampleModulePresenter(IBus bus) : Presenter(bus) {
    public string Status { get; private set; } = "";
    public OpState Load { get; } = new();

    public Task LoadAsync() => Run(Load, async () => {
        var response = await Bus.Send(new GetStatusRequest());
        Status = response.Status;
    });
}
