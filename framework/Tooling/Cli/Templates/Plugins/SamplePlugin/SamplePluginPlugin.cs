using VSA.Module;

namespace SamplePlugin;

// The root of the SamplePlugin plugin module. Each module gets the layers of the plugin module, and the host
// configures the plugin modules before the modules.
public sealed class SamplePluginPlugin : Plugin {
    public override void Configure(ModuleBuilder module) {
    }
}
