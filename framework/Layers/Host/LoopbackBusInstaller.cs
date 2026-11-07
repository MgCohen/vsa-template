using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;
using VSA.Presenters;

namespace VSA.Host;

public sealed class LoopbackBusInstaller(IServiceCollection services) : IVsaInstaller {
    public void Install(VsaContext context) =>
        services.AddHttpClient<IBus, HttpBus>((provider, client) => client.BaseAddress = LoopbackAddress(provider));

    static Uri LoopbackAddress(IServiceProvider provider) {
        var addresses = provider.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()?.Addresses ?? [];
        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.Ordinal))
            ?? addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("The server has no address for the bus.");
        foreach (var wildcard in new[] { "://+", "://*", "://[::]", "://0.0.0.0" }) {
            address = address.Replace(wildcard, "://localhost", StringComparison.Ordinal);
        }
        return new Uri(address);
    }
}
