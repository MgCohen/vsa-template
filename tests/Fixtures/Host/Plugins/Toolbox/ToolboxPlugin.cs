using Microsoft.Extensions.DependencyInjection;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

namespace Toolbox;

// A plugin module root with no layers. Each module root of the app gets it from Umpire.Sdk.
public sealed class ToolboxPlugin : Plugin {
    public override void Configure(IServiceCollection services) { }
}
