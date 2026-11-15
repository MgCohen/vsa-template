namespace VSA.Kernel;

using VSA.Architecture;

public class Pack<TArchitecture, THost> : Pack
    where TArchitecture : Architecture, new()
    where THost : AppHost, new() {
    readonly TArchitecture architecture = new();

    public override Architecture Architecture => architecture;

    public override AppHost CreateHost() => new THost();
}
