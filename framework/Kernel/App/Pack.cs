namespace Umpire.Kernel;

using Umpire.Markers;
using Umpire.Architecture;

[RequireDerived]
public abstract class Pack : Block<PackArchitecture> {
    public abstract Architecture Architecture { get; }

    public abstract AppHost CreateHost();
}
