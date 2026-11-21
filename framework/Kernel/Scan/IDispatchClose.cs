namespace Umpire.Kernels;

public interface IDispatchClose {
    Type Message { get; }

    IReadOnlyList<Type> ClosedServices { get; }
}
