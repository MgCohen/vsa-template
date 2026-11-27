using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Tests.Shared;

namespace Consumer.Tests;

[Category("Package")]
public sealed partial class NotesConsumerTests {
    const string Summary = "umpire-engine: ";

    [Test]
    [CancelAfter(20 * 60 * 1000)]
    public async Task NotesBuildsFromThePackagesRunsAndChecksItsArchitecture(CancellationToken ct) {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Notes"));
        UsePackages(workspace.Src);

        var clean = workspace.Dotnet("build", "Notes.csproj", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
        Assert.That(clean.Output, Does.Contain(Summary).And.Contain("0 errors"));
        var runtimeDependencies = File.ReadAllText(Path.Combine(workspace.Src, "artifacts~", "bin", "Notes", "release", "Notes.deps.json"));
        Assert.That(runtimeDependencies, Does.Contain("\"Umpire/").And.Contain("\"Umpire.Vsa/").And.Contain("\"Umpire.Web/").And.Not.Contain("Umpire.Vsa.Definition").And.Not.Contain("ArchUnitNET").And.Not.Contain("Mono.Cecil"));

        var url = $"http://127.0.0.1:{FreePort()}";
        using var app = workspace.Start("run", "--project", "Notes.csproj", "-c", "Release", "--no-build", "--no-launch-profile", "--urls", url);
        var output = new StringBuilder();
        app.OutputDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        app.ErrorDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        app.BeginOutputReadLine();
        app.BeginErrorReadLine();
        try {
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            await WaitUntilStarted(client, app, output, ct);

            var title = $"consumer note {Guid.NewGuid():N}";
            var created = await client.PostAsJsonAsync("/api/notebook/create-note", new { AuthorId = Guid.NewGuid(), Title = title }, ct);
            Assert.True(created.IsSuccessStatusCode, $"POST /api/notebook/create-note gave {created.StatusCode}.\n{output}");

            var page = await client.GetStringAsync("/", ct);
            Assert.That(page, Does.Contain("<h1>Notes</h1>"));
            Assert.That(page, Does.Contain(title));
        } finally {
            app.Kill(entireProcessTree: true);
            await app.WaitForExitAsync(CancellationToken.None);
        }

        TheBuildRunsTheRuleEngine(workspace);
    }

    static void TheBuildRunsTheRuleEngine(Workspace workspace) {
        DotnetResult Build(params string[] args) =>
            workspace.Dotnet(["build", "Notes.csproj", "-c", "Release", "--no-restore", "-nodeReuse:false", .. args]);

        var again = Build().EnsureSuccess();
        Assert.That(again.Output, Does.Contain("UMP030201"), "A build with no change runs the check again.");

        var probe = Path.Combine(Directory.CreateDirectory(Path.Combine(workspace.Src, "App", "Host")).FullName, "Probe.cs");
        File.WriteAllText(probe, """
            namespace Notes.Host;

            public static class Probe {
                public static Notes.Features.Notebook.Slices.CreateNote.CreateNoteRequest? Request { get; set; }
            }
            """);
        var finding = $"{probe}(4,90): error UMP020101: Probe in Host";

        var build = Build("-p:UmpireCheckMode=Error");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Contain(finding));
        Assert.That(build.Output, Does.Not.Contain("UMP030004"), "A finding is not an engine crash.");

        var warned = Build().EnsureSuccess();
        Assert.That(warned.Output, Does.Contain($"{probe}(4,90): warning UMP020101: Probe in Host"));

        File.Delete(probe);
        var domainProbe = Path.Combine(workspace.Src, "App", "Features", "Notebook", "Domain", "SenderProbe.cs");
        File.WriteAllText(domainProbe, """
            namespace Notes.Features.Notebook.Domain;

            public static class SenderProbe {
                public static Umpire.Vsa.Contracts.Requests.IRequestSender? Sender { get; set; }
            }
            """);

        var layer = Build("-p:UmpireCheckMode=Error");
        Assert.That(layer.Succeeded, Is.False, layer.Output);
        Assert.That(layer.Output, Does.Contain($"{domainProbe}(").And.Contain("error UMP020101: SenderProbe in Domain"),
            "A use of a layer in Umpire.Vsa.dll is a finding.");

        var skipped = Build("-p:UmpireCheckMode=Error", "-p:UmpireCheck=false");
        Assert.That(skipped.Succeeded, Is.True, skipped.Output);
        Assert.That(skipped.Output, Does.Not.Contain("UMP020101"), "UmpireCheck=false runs no check.");

        File.Delete(domainProbe);
        Build().EnsureSuccess();
    }

    const string WebProject = "<Project Sdk=\"Microsoft.NET.Sdk.Web\">";

    internal static void UsePackages(string root) {
        foreach (var file in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)) {
            var text = File.ReadAllText(file);
            if (text.Contains("Umpire.Sdk.Dev.props") && text.StartsWith(WebProject, StringComparison.Ordinal)) {
                text = DevImport().Replace(text, "");
                text = "<Project Sdk=\"Umpire.Sdk.Web\">" + text[WebProject.Length..];
            } else if (text.Contains("Umpire.Sdk.Dev.props") && !text.StartsWith("<Project>", StringComparison.Ordinal)) {
                text = DevImport().Replace(text, "");
                text = text.Insert(text.IndexOf('>') + 1, "\n  <Sdk Name=\"Umpire.Sdk\" />");
            } else if (text.Contains("Umpire.Sdk.Dev.props")) {
                text = DevImport().Replace(text, "");
                text = text.Replace("<Project>", "<Project Sdk=\"Umpire.Sdk\">");
                text = EmptyProject().Replace(text, "<Project Sdk=\"Umpire.Sdk\" />");
            }
            text = FrameworkReference().Replace(text, "<PackageReference Include=\"$1\" />");
            Assert.That(text, Does.Not.Contain("UmpireFrameworkSourceDir").And.Not.Contain("UmpireSdkDir"));
            File.WriteAllText(file, text);
        }
    }

    static async Task WaitUntilStarted(HttpClient client, System.Diagnostics.Process app, StringBuilder output, CancellationToken ct) {
        var until = DateTime.UtcNow.AddMinutes(3);
        var last = "no response";
        while (DateTime.UtcNow < until) {
            Assert.That(app.HasExited, Is.False, $"The app stopped with exit code {(app.HasExited ? app.ExitCode : 0)}.\n{output}");
            using var request = CancellationTokenSource.CreateLinkedTokenSource(ct);
            request.CancelAfter(TimeSpan.FromSeconds(10));
            try {
                using var response = await client.GetAsync("/api/notebook/list-notes", request.Token);
                if (response.StatusCode == HttpStatusCode.OK) {
                    return;
                }
                last = $"status {(int)response.StatusCode} {response.StatusCode}";
            } catch (Exception e) when (e is HttpRequestException || (e is OperationCanceledException && !ct.IsCancellationRequested)) {
                last = $"{e.GetType().Name}: {e.Message}";
            }
            await Task.Delay(500, ct);
        }
        Assert.Fail($"The app did not start in three minutes. The last GET /api/notebook/list-notes gave {last}.\n{output}");
    }

    static int FreePort() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [GeneratedRegex(@"\s*<Import Project=""[^""]*(Umpire\.Sdk\.Dev\.props|Sdk\.targets)[^""]*"" />")]
    private static partial Regex DevImport();

    [GeneratedRegex(@"<Project Sdk=""Umpire\.Sdk"">\s*</Project>")]
    private static partial Regex EmptyProject();

    [GeneratedRegex(@"<ProjectReference Include=""\$\(UmpireFrameworkSourceDir\)[^""]*/(Umpire\.[\w.]+)\.csproj"" />")]
    private static partial Regex FrameworkReference();
}
