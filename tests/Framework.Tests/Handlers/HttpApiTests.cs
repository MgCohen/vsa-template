using System.Net;
using System.Net.Http.Json;
using Answers;
using Answers.Slices.Answer;
using Answers.Slices.GetItem;
using Answers.Slices.ListAll;
using Framework.Tests.Host;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Framework.Tests.Handlers;

public sealed class HttpApiTests {
    IHost app = null!;
    HttpClient client = null!;

    [SetUp]
    public async Task Start() {
        app = await WebApps.OnTestServer(new AnswersModule()).Start();
        client = app.GetTestClient();
    }

    [TearDown]
    public void Stop() {
        client.Dispose();
        app.Dispose();
    }

    [Test]
    public async Task Success_is_200_with_the_response() {
        var response = await client.PostAsJsonAsync(AnswerRequest.Route, new AnswerRequest(null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadFromJsonAsync<AnswerResponse>(), Is.EqualTo(new AnswerResponse("done")));
    }

    [TestCase("notes.not_found", HttpStatusCode.NotFound)]
    [TestCase("notes.title_empty", HttpStatusCode.BadRequest)]
    [TestCase("notes.not_found_here", HttpStatusCode.BadRequest)]
    public async Task An_error_is_a_problem_with_the_status_from_the_code(string code, HttpStatusCode status) {
        var response = await client.PostAsJsonAsync(AnswerRequest.Route, new AnswerRequest(code));

        Assert.That(response.StatusCode, Is.EqualTo(status));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem?.Title, Is.EqualTo(code));
        Assert.That(problem?.Detail, Is.EqualTo("The test error."));
    }

    [Test]
    public async Task A_get_request_reads_the_route_and_the_query() {
        var id = Guid.NewGuid();

        var response = await client.GetFromJsonAsync<GetItemResponse>($"/items/{id}?count=4&loud=true");

        Assert.That(response, Is.EqualTo(new GetItemResponse(id, 4, true)));
    }

    [Test]
    public async Task A_request_with_no_properties_is_bound() {
        var response = await client.GetFromJsonAsync<ListAllResponse>(ListAllRequest.Route);

        Assert.That(response, Is.EqualTo(new ListAllResponse(3)));
    }

    [Test]
    public async Task A_value_of_the_wrong_type_is_a_problem() {
        var response = await client.GetAsync($"/items/{Guid.NewGuid()}?count=many");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem?.Title, Is.EqualTo("request.not_valid"));
    }
}
