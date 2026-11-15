namespace VSA.Kernel;

using Microsoft.Extensions.DependencyInjection;
using VSA.Analyzers.Markers;

[RequireDerived]
public abstract class Module {
    public string Name => AppTypes.NameOf(GetType());

    protected internal virtual int Order => 1;

    protected internal virtual string Section => "Modules";

    public abstract void Configure(IServiceCollection services);
}
