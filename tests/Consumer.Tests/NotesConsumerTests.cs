using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Tests.Shared;

namespace Consumer.Tests;

// Builds samples/Notes as a consumer does: from the packed framework packages and VSA.Sdk, not from the source.
public sealed partial class NotesConsumerTests {
    [Fact]
    public async Task NotesBuildsFromThePackagesAndRuns() {
        var ct = TestContext.Current.CancellationToken;
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
            Assert.Contains("<h1>Notes</h1>", page);
            Assert.Contains(title, page);
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
            Assert.DoesNotContain("VsaFrameworkSourceDir", text);
            Assert.DoesNotContain("VsaSdkDir", text);
            File.WriteAllText(file, text);
        }
    }

    static async Task WaitUntilStarted(HttpClient client, System.Diagnostics.Process app, StringBuilder output, CancellationToken ct) {
        var until = DateTime.UtcNow.AddMinutes(1);
        while (DateTime.UtcNow < until) {
            Assert.False(app.HasExited, $"The app stopped with exit code {(app.HasExited ? app.ExitCode : 0)}.\n{output}");
            try {
                if ((await client.GetAsync("/api/notes", ct)).StatusCode == HttpStatusCode.OK) {
                    return;
                }
            } catch (HttpRequestException) {
            }
            await Task.Delay(500, ct);
        }
        Assert.Fail($"The app did not start in one minute.\n{output}");
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
