using VSA.Analyzers.Markers;
using VSA.Architecture;
using VSA.Kernel;

namespace VSA.Composition;

[RequireDerived]
public abstract class VsaApp<TDefinition> : App, IArchitectureProvider where TDefinition : ArchitectureDefinition, new() {
    public virtual ArchitectureDefinition Architecture => new TDefinition();

    protected virtual IReadOnlyList<VSA.Module.Module>? Modules() => null;

    protected override AppBuilder Configure(AppBuilder builder) =>
        Modules() is { } modules ? builder.Modules([.. modules]) : builder.ScanFor(typeof(VSA.Module.Module));
}
