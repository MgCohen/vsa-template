namespace Till.Domain.Shared;

public interface IStock<T> where T : struct, IAmount {
    string Amount { get; }
}
