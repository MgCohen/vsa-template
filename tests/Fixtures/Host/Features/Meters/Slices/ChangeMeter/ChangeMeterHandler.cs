using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace Meters.Slices.ChangeMeter;

sealed class ChangeMeterHandler<T> : CommandHandler<ChangeMeterCommand<T>, string> where T : struct {
    public override Task<Result<string>> HandleAsync(Envelope<ChangeMeterCommand<T>> envelope) =>
        Task.FromResult<Result<string>>($"{typeof(T).Name} {envelope.Message.Delta}");
}
