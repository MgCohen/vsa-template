namespace Umpire.Kernel;

using Microsoft.Extensions.DependencyInjection;

public abstract class Module<TSettings> : Module where TSettings : class, new() {
    public sealed override void Configure(IServiceCollection services) =>
        Configure(services, ModuleSettings.Bind<TSettings>(services, $"{Section}:{Name}", Validate));

    public abstract void Configure(IServiceCollection services, TSettings settings);

    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
