using System.Net;
using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace Framework.Tests.Handlers;

public sealed class RoutedHandlerTests : IAsyncLifetime {
    WebApplication app = null!;
    HttpClient client = null!;

    public async ValueTask InitializeAsync() {
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

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task SuccessIs200WithTheResponse() {
        var response = await client.PostAsJsonAsync(AnswerRequest.Route, new AnswerRequest(null), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new AnswerResponse("done"), await response.Content.ReadFromJsonAsync<AnswerResponse>(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("notes.not_found", HttpStatusCode.NotFound)]
    [InlineData("notes.title_empty", HttpStatusCode.BadRequest)]
    [InlineData("notes.not_found_here", HttpStatusCode.BadRequest)]
    public async Task ErrorIsAProblemWithTheStatusFromTheCode(string code, HttpStatusCode status) {
        var response = await client.PostAsJsonAsync(AnswerRequest.Route, new AnswerRequest(code), TestContext.Current.CancellationToken);

        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(code, problem?.Title);
        Assert.Equal("The test error.", problem?.Detail);
    }

    public sealed record AnswerRequest(string? ErrorCode) : IRequest<AnswerResponse> {
        public static HttpMethod Method => HttpMethod.Post;
        public static string Route => "/answer";
    }

    public sealed record AnswerResponse(string Text);

    public sealed class AnswerHandler : RoutedHandler<AnswerRequest, AnswerResponse> {
        protected override Task<Result<AnswerResponse>> HandleRequestAsync(AnswerRequest request, CancellationToken ct) =>
            Task.FromResult<Result<AnswerResponse>>(request.ErrorCode is { } code
                ? new Error(code, "The test error.")
                : new AnswerResponse("done"));
    }
}
