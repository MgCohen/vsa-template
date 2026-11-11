using Microsoft.Extensions.DependencyInjection;
using Units.Infrastructure;
using VSA.Module;

namespace Units;

public sealed class UnitsPlugin : Plugin {
    public override void Configure(ModuleBuilder module) => module.Services.AddSingleton<MeterStore>();
}
