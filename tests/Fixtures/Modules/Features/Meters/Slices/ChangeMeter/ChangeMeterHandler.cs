namespace Meters.Slices.ChangeMeter;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class ChangeMeterHandler<T> : CommandHandler<ChangeMeterCommand<T>, string> where T : struct {
    public override Task<Result<string>> HandleAsync(IEnvelope<ChangeMeterCommand<T>> envelope) =>
        Task.FromResult<Result<string>>($"{typeof(T).Name} {envelope.Message.Delta}");
}
