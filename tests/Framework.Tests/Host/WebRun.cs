namespace Framework.Tests.Host;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

sealed class WebRun(AppHost host, IServiceProvider services) : IDisposable {
    public IServiceProvider Services { get; } = services;

    public HttpClient GetTestClient() => ((TestServer)Services.GetRequiredService<IServer>()).CreateClient();

    public IEnumerable<Microsoft.AspNetCore.Http.Endpoint> Endpoints() =>
        Services.GetRequiredService<EndpointDataSource>().Endpoints;

    public void Dispose() => host.Dispose();
}
