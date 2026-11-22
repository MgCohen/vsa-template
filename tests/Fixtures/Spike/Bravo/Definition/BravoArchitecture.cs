namespace Spike.Bravo.Definition;

using Umpire.Architecture;

public sealed class BravoArchitecture : Architecture {
    protected override void Configure(Options defaults) => defaults.DefaultPermission = Permission.Deny;

    protected override void Define(Block block) => block.Add<BravoParts.Definition>();
}
