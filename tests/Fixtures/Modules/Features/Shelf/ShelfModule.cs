namespace Shelf;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

// A module with only a Presenters layer: no Handlers/Requests and no Handlers/Events.
public sealed class ShelfModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
