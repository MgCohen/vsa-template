namespace Spike.Alpha.Definition;

using Spike.Bravo.Definition;
using Umpire.Architecture;
using static Spike.Alpha.Definition.AlphaParts;

public class AlphaArchitecture : Architecture {
    protected override void Configure(ArchitectureConfig config) => config.Defaults.DefaultPermission = Permission.Deny;

    protected override void Define(Part root, Part kernel) {
        root.Add<A1>().And<Zulu>();
        root.Add<A2>().Add<Store>();
        root.AddGroup<BravoArchitecture.Plugin>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<A2>().Uses<A1>().And<Store>();
        rules.For<A1>().Uses<Zulu>().And<BravoArchitecture.B1>();
        rules.For<BravoArchitecture.Plugin>().Uses<A1>().And<Zulu>();
    }
}
