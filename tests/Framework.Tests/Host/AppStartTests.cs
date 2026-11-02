using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Shelf;
using VSA.Host;

namespace Framework.Tests.Host;

public sealed class AppStartTests {
    [Test]
    public async Task AppWithZeroModulesStartsAndMapsNoEndpoints() {
        await using var app = await Start(builder => builder.AddVsa(Array.Empty<VSA.Module.Module>()));

        Assert.That(Endpoints(app), Is.Empty);
        Assert.That((await app.GetTestClient().GetAsync("/api/anything")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AppWithAModuleWithoutRequestHandlersStarts() {
        // Shelf has a Presenters layer and no Handlers/Requests layer.
        await using var app = await Start(builder => builder.AddVsa(new ShelfModule()));

        Assert.That(Endpoints(app), Is.Empty);
    }

    static async Task<WebApplication> Start(Action<WebApplicationBuilder> addVsa) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        addVsa(builder);
        var app = builder.Build();
        app.MapVsaRequests();
        await app.StartAsync();
        return app;
    }

    static IEnumerable<Endpoint> Endpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints);
}
