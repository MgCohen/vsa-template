namespace Till.Domain;

using Till.Domain.Shared;

public sealed class Stock<T> : IStock<T> where T : struct, IAmount {
    public string Amount => typeof(T).Name;
}
