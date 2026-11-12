using Units.Domain;
using Units.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace UsesPlugins.Slices.ReadMeter;

// A module handler uses the Domain and the Infrastructure of a plugin module, with no project of its own for them.
sealed class ReadMeterHandler(MeterStore store) : CommandHandler<ReadMeterCommand, int> {
    public override Task<Result<int>> HandleAsync(Envelope<ReadMeterCommand> envelope) {
        Meter meter = store.Read();
        return Task.FromResult<Result<int>>(meter.Value);
    }
}
