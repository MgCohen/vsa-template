namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;

public abstract class Module<TSettings> : Module where TSettings : class, new() {
    public sealed override void Configure(IServiceCollection services) {
    }

    public abstract void Configure(IServiceCollection services, TSettings settings);

    internal sealed override void Compose(IServiceCollection services, ModuleSettings settings) =>
        Configure(services, settings.Use<TSettings>(services, this, Validate));

    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
