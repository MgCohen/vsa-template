namespace VSA.Kernel;

using VSA.Analyzers.Markers;
using VSA.Architecture;

[RequireDerived]
public abstract class Pack {
    public abstract Architecture Architecture { get; }

    public abstract AppHost CreateHost();
}
