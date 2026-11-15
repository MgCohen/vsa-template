namespace Umpire.Kernel;

using Umpire.Markers;
using Umpire.Architecture;

[RequireDerived]
public abstract class Pack {
    public abstract Architecture Architecture { get; }

    public abstract AppHost CreateHost();
}
