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

        Assert.That(Endpoints(app), Is.Empty);
        Assert.That((await app.GetTestClient().GetAsync("/api/anything")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AppWithAModuleWithoutRequestHandlersStarts() {
        using var app = await WebApps.OnTestServer(new ShelfModule()).Start();

        Assert.That(Endpoints(app), Is.Empty);
    }

    [Test]
    public async Task AppMapsTheRouteOfEachRequest() {
        using var app = await WebApps.OnTestServer(new AnswersModule()).Start();

        var routes = Endpoints(app).OfType<RouteEndpoint>().Select(Route);

        Assert.That(routes, Is.EquivalentTo(new[] {
            "POST /api/answers/answer",
            "GET /api/answers/get-item",
            "GET /api/answers/list-all",
        }));
    }

    static string Route(RouteEndpoint endpoint) =>
        $"{endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Single()} {endpoint.RoutePattern.RawText}";

    static IEnumerable<Endpoint> Endpoints(WebRun app) => app.Endpoints();
}
