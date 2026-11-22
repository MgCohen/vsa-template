namespace Till.Domain;

using Till.Domain.Shared;

public sealed class Exchange<TFrom, TTo> : IExchange<TFrom, TTo>
    where TFrom : struct, IAmount
    where TTo : struct, IAmount {
    public string Rate => $"{typeof(TFrom).Name}->{typeof(TTo).Name}";
}
