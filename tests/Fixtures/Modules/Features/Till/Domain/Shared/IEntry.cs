namespace Till.Domain.Shared;

public interface IEntry<T> {
    Type Entry { get; }
}
