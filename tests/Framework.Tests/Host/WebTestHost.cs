namespace Framework.Tests.Host;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Hosts.Web;
using Umpire.Kernels;

sealed class WebTestHost(string[] args) : WebHost(args) {
    public List<Action<IServiceCollection>> Actions { get; } = [];

    protected override AppTypes TypesOf(App app) => new(ModuleProvider);

    protected override void Configure(IServiceCollection services, AppTypes types) {
        services.AddSingleton<IServer, TestServer>();
        foreach (var add in Actions) {
            add(services);
        }
    }
}
