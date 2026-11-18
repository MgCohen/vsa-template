namespace Umpire.Kernels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Markers;

[RequireDerived]
public abstract class Module {
    public string Name => AppTypes.NameOf(GetType());

    protected internal virtual int Order => 1;

    protected internal virtual string Section => "Modules";

    public virtual void Configure(IServiceCollection services) {
    }

    internal virtual void Compose(IServiceCollection services, IConfiguration configuration) => Configure(services);
}
