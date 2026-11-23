namespace Meters.Slices.ChangeMeter;

using Meters.Domain.Shared;
using Umpire.Markers;

[AutoRegister]
public interface IMeterRule {
    string Meter { get; }
}

sealed class MeterRule<T> : IMeterRule where T : struct, IMeter {
    public string Meter => typeof(T).Name;
}

[AutoRegister(Lifetime.Singleton)]
sealed class MeterCache<T> where T : struct, IMeter {
    public Guid Id { get; } = Guid.NewGuid();
}

sealed class MeterHelper<T> where T : struct {
}
