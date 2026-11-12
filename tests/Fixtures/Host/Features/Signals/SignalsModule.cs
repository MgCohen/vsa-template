using Microsoft.Extensions.DependencyInjection;
using Signals.Slices.Ring;
using VSA.Module;

namespace Signals;

// A module with an event, its handler, a helper class next to the handler, and a presenter that shows the event.
public sealed class SignalsModule : Module {
    public override void Configure(ModuleBuilder module) => module.Services.AddSingleton<RingLog>();
}
