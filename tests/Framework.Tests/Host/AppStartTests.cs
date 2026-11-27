namespace Framework.Tests.Host;

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Answers;
using Answers.Slices.Answer;
using Answers.Slices.GetItem;
using Answers.Slices.ListAll;
using Shelf;

public sealed class AppStartTests {
    [Test]
    public async Task AppWithZeroModulesStartsAndMapsNoEndpoints() {
        using var app = await WebApps.OnTestServer().Start();

        Assert.That(Endpoints(app).OfType<RouteEndpoint>().Select(e => e.RoutePattern.RawText), Is.EqualTo(new[] { "/api/{**name}" }));
        Assert.That((await app.GetTestClient().GetAsync("/api/anything")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AppWithAModuleWithoutRequestHandlersStarts() {
        using var app = await WebApps.OnTestServer(new ShelfModule()).Start();

        Assert.That(Endpoints(app).OfType<RouteEndpoint>().Select(e => e.RoutePattern.RawText), Is.EqualTo(new[] { "/api/{**name}" }));
    }

    [Test]
    public async Task AppMapsOneRouteForAllRequests() {
        using var app = await WebApps.OnTestServer(new AnswersModule()).Start();

        var route = Endpoints(app).OfType<RouteEndpoint>().Single();

        Assert.That(route.RoutePattern.RawText, Is.EqualTo("/api/{**name}"));
        Assert.That(route.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods,
            Is.EquivalentTo(new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }));
    }

    static IEnumerable<Endpoint> Endpoints(WebRun app) => app.Endpoints();
}
