using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

namespace Bare;

// A module root with no layer projects. Its root records no layers.
public sealed class BareModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
