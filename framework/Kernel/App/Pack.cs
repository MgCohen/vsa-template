namespace Umpire.Kernels;

using Umpire.Architecture;

public abstract class Pack : Block<PackArchitecture> {
    public abstract Architecture Architecture { get; }

    public abstract Kernel Kernel { get; }

    public abstract AppHost CreateHost();
}
