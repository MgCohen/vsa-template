using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Shelf;
using Microsoft.Extensions.Hosting;

namespace Framework.Tests.Host;

public sealed class AppStartTests {
    [Test]
    public async Task AppWithZeroModulesStartsAndMapsNoEndpoints() {
        using var app = await WebApps.OnTestServer().Start();

        Assert.That(Endpoints(app), Is.Empty);
        Assert.That((await app.GetTestClient().GetAsync("/api/anything")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AppWithAModuleWithoutRequestHandlersStarts() {
        // Shelf has a Presenters layer and no Handlers/Requests layer.
        using var app = await WebApps.OnTestServer(new ShelfModule()).Start();

        Assert.That(Endpoints(app), Is.Empty);
    }

    static IEnumerable<Endpoint> Endpoints(IHost app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints);
}
