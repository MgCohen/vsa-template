namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

public abstract class Plugin<TSettings> : Plugin where TSettings : class, new() {
    public sealed override void Configure(IServiceCollection services) {
    }

    public abstract void Configure(IServiceCollection services, TSettings settings);

    internal sealed override void Compose(IServiceCollection services, IConfiguration configuration) {
        var section = configuration.GetSection($"{Section}:{Name}");
        Configure(services, ModuleSettings.Bind<TSettings>(services, section, Validate));
    }

    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
