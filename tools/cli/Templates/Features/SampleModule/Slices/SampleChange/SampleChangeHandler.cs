namespace SampleApp.Features.SampleModule.Slices.SampleChange;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class SampleChangeHandler : CommandHandler<SampleChangeCommand, SampleChangeResult> {
    public override Task<Result<SampleChangeResult>> HandleAsync(Envelope<SampleChangeCommand> envelope) =>
        Task.FromResult<Result<SampleChangeResult>>(new SampleChangeResult());
}
