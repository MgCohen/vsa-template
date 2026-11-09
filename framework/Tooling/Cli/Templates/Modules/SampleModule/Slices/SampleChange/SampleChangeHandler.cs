using VSA.Contracts;
using VSA.Handlers.Commands;

namespace SampleModule.Slices.SampleChange;

sealed class SampleChangeHandler : CommandHandler<SampleChangeCommand, SampleChangeResult> {
    public override Task<Result<SampleChangeResult>> HandleAsync(Envelope<SampleChangeCommand> envelope) =>
        Task.FromResult<Result<SampleChangeResult>>(new SampleChangeResult());
}
