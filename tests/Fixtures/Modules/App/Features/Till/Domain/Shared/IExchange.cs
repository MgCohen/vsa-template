namespace Till.Domain.Shared;

using Umpire.Markers;

[AutoRegister]
public interface IExchange<TFrom, TTo> {
    string Rate { get; }
}
