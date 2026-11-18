namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public abstract class Plugin<TSettings> : Plugin where TSettings : class, new() {
    public sealed override void Configure(IServiceCollection services) {
    }

    public abstract void Configure(IServiceCollection services, TSettings settings);

    internal sealed override void Compose(IServiceCollection services, ModuleSettings settings) =>
        Configure(services, settings.Use<TSettings>(services, this, Validate));

    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
