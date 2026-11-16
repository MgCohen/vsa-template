namespace Umpire.Kernel;

using Umpire.Architecture;

public abstract class Pack<TArchitecture, THost> : Pack
    where TArchitecture : Architecture, new()
    where THost : AppHost, new() {
    readonly TArchitecture architecture = new();

    public override Architecture Architecture => architecture;

    public override AppHost CreateHost() => new THost();
}
