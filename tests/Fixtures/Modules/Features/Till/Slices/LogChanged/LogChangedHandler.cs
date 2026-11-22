namespace Till.Slices.LogChanged;

using Till.Domain;
using Till.Domain.Shared;
using Till.Slices.Change;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;

sealed class LogChangedHandler<T>(TillLog log) : IntegrationEventHandler<ChangedEvent<T>> where T : struct, IAmount {
    public override Task<Result<Unit>> HandleAsync(IEnvelope<ChangedEvent<T>> envelope) {
        log.Lines.Add($"changed:{typeof(T).Name}:{envelope.Message.Amount}");
        return Done;
    }
}
