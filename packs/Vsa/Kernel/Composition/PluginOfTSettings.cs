namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

public abstract class Plugin<TSettings> : Plugin where TSettings : class, new() {
    public sealed override void Configure(IServiceCollection services) =>
        Configure(services, ModuleSettings.Bind<TSettings>(services, $"{Section}:{Name}", Validate));

    public abstract void Configure(IServiceCollection services, TSettings settings);

    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
