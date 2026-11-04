using System.Net;
using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Framework.Tests.Handlers;

public sealed class RoutedHandlerTests {
    WebApplication app = null!;
    HttpClient client = null!;

    [SetUp]
    public async Task Start() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = [typeof(AnswerHandler).Assembly];
        });
        app = builder.Build();
        app.UseFastEndpoints();
        await app.StartAsync();
        client = app.GetTestClient();
    }

    [TearDown]
    public async Task Stop() {
        client.Dispose();
        await app.DisposeAsync();
    }

    [Test]
    public async Task SuccessIs200WithTheResponse() {
        var response = await client.PostAsJsonAsync(AnswerRequest.Route, new AnswerRequest(null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadFromJsonAsync<AnswerResponse>(), Is.EqualTo(new AnswerResponse("done")));
    }

    [TestCase("notes.not_found", HttpStatusCode.NotFound)]
    [TestCase("notes.title_empty", HttpStatusCode.BadRequest)]
    [TestCase("notes.not_found_here", HttpStatusCode.BadRequest)]
    public async Task ErrorIsAProblemWithTheStatusFromTheCode(string code, HttpStatusCode status) {
        var response = await client.PostAsJsonAsync(AnswerRequest.Route, new AnswerRequest(code));

        Assert.That(response.StatusCode, Is.EqualTo(status));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem?.Title, Is.EqualTo(code));
        Assert.That(problem?.Detail, Is.EqualTo("The test error."));
    }

    public sealed record AnswerRequest(string? ErrorCode) : IRequest<AnswerResponse> {
        public static HttpMethod Method => HttpMethod.Post;
        public static string Route => "/answer";
    }

    public sealed record AnswerResponse(string Text) : IResponse;

    public sealed class AnswerHandler : RoutedHandler<AnswerRequest, AnswerResponse> {
        protected override Task<Result<AnswerResponse>> HandleRequestAsync(AnswerRequest request, CancellationToken ct) =>
            Task.FromResult<Result<AnswerResponse>>(request.ErrorCode is { } code
                ? new Error(code, "The test error.")
                : new AnswerResponse("done"));
    }
}
