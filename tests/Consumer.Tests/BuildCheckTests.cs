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

        var probe = Path.Combine(workspace.Src, "App", "Host", "Probe.cs");
        File.WriteAllText(probe, """
            namespace Notes.App.Host;

            public static class Probe {
                public static Notes.Features.Notebook.Slices.CreateNote.CreateNoteRequest? Request { get; set; }
            }
            """);
        var finding = $"{probe}(4,90): error UMP020101: Probe in Host";

        var build = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Contain(finding));

        var warned = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheckMode=Warn")
            .EnsureSuccess();
        Assert.That(warned.Output, Does.Contain($"{probe}(4,90): warning UMP020101: Probe in Host"));

        File.Delete(probe);
        workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
    }
}
