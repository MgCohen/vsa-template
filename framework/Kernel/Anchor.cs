using VSA.Analyzers.Markers;

namespace VSA.Kernel;

[RequireDerived]
public abstract class Anchor {
    public string Name => AnchorRoot.NameOf(GetType());

    protected internal virtual int Order => 0;

    protected internal virtual string Section => "Anchors";

    public abstract void Configure(AnchorBuilder anchor);
}
