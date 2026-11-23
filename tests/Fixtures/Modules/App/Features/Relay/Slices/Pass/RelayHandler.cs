namespace Relay.Slices.Pass;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class RelayHandler<T> : CommandHandler<RelayCommand<T>, string> {
    public override Task<Result<string>> HandleAsync(IEnvelope<RelayCommand<T>> envelope) =>
        Task.FromResult<Result<string>>($"{typeof(T).Name} {envelope.Message.Value}");
}
