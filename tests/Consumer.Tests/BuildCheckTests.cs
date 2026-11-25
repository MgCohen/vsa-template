using Tests.Shared;

namespace Consumer.Tests;

public sealed class BuildCheckTests {
    const string Summary = "umpire-engine: ";

    [Test]
    [CancelAfter(20 * 60 * 1000)]
    public void The_build_of_the_host_runs_the_rule_engine(CancellationToken ct) {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Notes"));
        NotesConsumerTests.UsePackages(workspace.Src);

        var clean = workspace.Dotnet("build", "Notes.csproj", "-nodeReuse:false").EnsureSuccess();
        Assert.That(clean.Output, Does.Contain(Summary).And.Contain("0 errors"));

        var again = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
        Assert.That(again.Output, Does.Contain("UMP030201"), "A build with no change runs the check again.");

        var probe = Path.Combine(Directory.CreateDirectory(Path.Combine(workspace.Src, "App", "Host")).FullName, "Probe.cs");
        File.WriteAllText(probe, """
            namespace Notes.Host;

            public static class Probe {
                public static Notes.Features.Notebook.Slices.CreateNote.CreateNoteRequest? Request { get; set; }
            }
            """);
        var finding = $"{probe}(4,90): error UMP020101: Probe in Host";

        var build = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheckMode=Error");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Contain(finding));
        Assert.That(build.Output, Does.Not.Contain("UMP030004"), "A finding is not an engine crash.");

        var warned = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false")
            .EnsureSuccess();
        Assert.That(warned.Output, Does.Contain($"{probe}(4,90): warning UMP020101: Probe in Host"));

        File.Delete(probe);
        var domainProbe = Path.Combine(workspace.Src, "App", "Features", "Notebook", "Domain", "SenderProbe.cs");
        File.WriteAllText(domainProbe, """
            namespace Notes.Features.Notebook.Domain;

            public static class SenderProbe {
                public static Umpire.Vsa.Contracts.Requests.IRequestSender? Sender { get; set; }
            }
            """);

        var layer = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheckMode=Error");
        Assert.That(layer.Succeeded, Is.False, layer.Output);
        Assert.That(layer.Output, Does.Contain($"{domainProbe}(").And.Contain("error UMP020101: SenderProbe in Domain"),
            "A use of a layer in Umpire.dll is a finding.");

        File.Delete(domainProbe);
        workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
    }
}
