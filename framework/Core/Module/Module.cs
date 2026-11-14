using VSA.Analyzers.Markers;
using VSA.Kernel;

namespace VSA.Module;

// One class at the root of each module. The host calls Configure at start-up.
[RequireDerived]
public abstract class Module : Anchor {
    protected internal override int Order => 1;

    protected internal override string Section => "Modules";

    public abstract override void Configure(AnchorBuilder module);
}
