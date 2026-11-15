namespace SampleApp.Plugins.SamplePlugin;

using Microsoft.Extensions.DependencyInjection;
using VSA.Kernel;

// The root of the SamplePlugin plugin module. Each module gets the layers of the plugin module, and the host
// configures the plugin modules before the modules.
public sealed class SamplePluginPlugin : Plugin {
    public override void Configure(IServiceCollection services) {
    }
}
