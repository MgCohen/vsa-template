namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;

public abstract class Module {
    public string Name => AppTypes.NameOf(GetType());

    protected internal virtual int Order => 1;

    public void Install(IServiceCollection services, ModuleSettings settings) => InstallWith(services, settings);

    internal virtual void InstallWith(IServiceCollection services, ModuleSettings settings) => Configure(services);

    protected virtual void Configure(IServiceCollection services) {
    }
}
