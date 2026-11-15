using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

namespace Shelf;

// A module with only a Presenters layer: no Handlers/Requests and no Handlers/Events.
public sealed class ShelfModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
