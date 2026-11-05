using VSA.Handlers.Commands;

namespace SampleModule.Slices.SampleChange;

sealed class SampleChangeHandler : CommandHandler<SampleChangeCommand, SampleChangeResult> {
    public override Task<SampleChangeResult> HandleAsync(SampleChangeCommand command, CancellationToken ct) =>
        Task.FromResult(new SampleChangeResult());
}
