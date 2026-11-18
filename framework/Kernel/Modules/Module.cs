namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Markers;

[RequireDerived]
public abstract class Module {
    public string Name => AppTypes.NameOf(GetType());

    protected internal virtual int Order => 1;

    public virtual void Configure(IServiceCollection services) {
    }

    internal virtual void Compose(IServiceCollection services, ModuleSettings settings) => Configure(services);
}
