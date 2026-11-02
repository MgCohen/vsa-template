using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Tests.Shared;

namespace Consumer.Tests;

// Builds samples/Notes as a consumer does: from the packed framework packages and VSA.Sdk, not from the source.
public sealed partial class NotesConsumerTests {
    [Test]
    [CancelAfter(10 * 60 * 1000)]
    public async Task NotesBuildsFromThePackagesAndRuns(CancellationToken ct) {
        using var workspace = Workspace.Create(Repo.Path("samples", "Notes"));
        UsePackages(workspace.Src);

        workspace.Dotnet("build", "App/Host", "-c", "Release", "-nodeReuse:false").EnsureSuccess();

        var url = $"http://127.0.0.1:{FreePort()}";
        using var app = workspace.Start("run", "--project", "App/Host", "-c", "Release", "--no-build", "--no-launch-profile", "--urls", url);
        var output = new StringBuilder();
        app.OutputDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        app.ErrorDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        app.BeginOutputReadLine();
        app.BeginErrorReadLine();
        try {
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            await WaitUntilStarted(client, app, output, ct);

            var title = $"consumer note {Guid.NewGuid():N}";
            var created = await client.PostAsJsonAsync("/api/notes", new { AuthorId = Guid.NewGuid(), Title = title }, ct);
            Assert.True(created.IsSuccessStatusCode, $"POST /api/notes gave {created.StatusCode}.\n{output}");

            var page = await client.GetStringAsync("/", ct);
            Assert.That(page, Does.Contain("<h1>Notes</h1>"));
            Assert.That(page, Does.Contain(title));
        } finally {
            app.Kill(entireProcessTree: true);
            await app.WaitForExitAsync(CancellationToken.None);
        }
    }

    // Changes the projects from the source mode of this repository to the consumer form.
    // A module project becomes <Project Sdk="VSA.Sdk">. A reference to a framework project becomes a package reference.
    static void UsePackages(string root) {
        foreach (var file in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)) {
            var text = File.ReadAllText(file);
            if (text.Contains("VSA.Sdk.Dev.props")) {
                text = DevImport().Replace(text, "");
                text = text.Replace("<Project>", "<Project Sdk=\"VSA.Sdk\">");
                text = EmptyProject().Replace(text, "<Project Sdk=\"VSA.Sdk\" />");
            }
            text = FrameworkReference().Replace(text, "<PackageReference Include=\"$1\" />");
            Assert.That(text, Does.Not.Contain("VsaFrameworkSourceDir").And.Not.Contain("VsaSdkDir"));
            File.WriteAllText(file, text);
        }
    }

    // The first start after a cold restore can be slow, so the wait is long. Each request has its own short timeout.
    static async Task WaitUntilStarted(HttpClient client, System.Diagnostics.Process app, StringBuilder output, CancellationToken ct) {
        var until = DateTime.UtcNow.AddMinutes(3);
        var last = "no response";
        while (DateTime.UtcNow < until) {
            Assert.That(app.HasExited, Is.False, $"The app stopped with exit code {(app.HasExited ? app.ExitCode : 0)}.\n{output}");
            using var request = CancellationTokenSource.CreateLinkedTokenSource(ct);
            request.CancelAfter(TimeSpan.FromSeconds(10));
            try {
                using var response = await client.GetAsync("/api/notes", request.Token);
                if (response.StatusCode == HttpStatusCode.OK) {
                    return;
                }
                last = $"status {(int)response.StatusCode} {response.StatusCode}";
            } catch (Exception e) when (e is HttpRequestException || (e is OperationCanceledException && !ct.IsCancellationRequested)) {
                last = $"{e.GetType().Name}: {e.Message}";
            }
            await Task.Delay(500, ct);
        }
        Assert.Fail($"The app did not start in three minutes. The last GET /api/notes gave {last}.\n{output}");
    }

    static int FreePort() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [GeneratedRegex(@"\s*<Import Project=""[^""]*(VSA\.Sdk\.Dev\.props|Sdk\.targets)[^""]*"" />")]
    private static partial Regex DevImport();

    [GeneratedRegex(@"<Project Sdk=""VSA\.Sdk"">\s*</Project>")]
    private static partial Regex EmptyProject();

    [GeneratedRegex(@"<ProjectReference Include=""\$\(VsaFrameworkSourceDir\)[^""]*/(VSA\.[\w.]+)\.csproj"" />")]
    private static partial Regex FrameworkReference();
}
