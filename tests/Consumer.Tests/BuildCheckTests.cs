using Tests.Shared;

namespace Consumer.Tests;

public sealed class BuildCheckTests {
    const string Summary = "umpire-engine: ";

    [Test]
    [CancelAfter(20 * 60 * 1000)]
    public void The_build_of_the_host_runs_the_rule_engine(CancellationToken ct) {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Notes"));
        NotesConsumerTests.UsePackages(workspace.Src);
        AddBuildHook(workspace.Src, "Notes.csproj");

        var clean = workspace.Dotnet("build", "Notes.csproj", "-nodeReuse:false").EnsureSuccess();
        Assert.That(clean.Output, Does.Contain(Summary).And.Contain("0 errors"));

        var again = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
        Assert.That(again.Output, Does.Not.Contain(Summary), "A build with no change runs the check again.");

        var probe = Path.Combine(workspace.Src, "App", "Host", "Probe.cs");
        File.WriteAllText(probe, """
            namespace Notes.App.Host;

            public static class Probe {
                public static Notes.Features.Notebook.Slices.CreateNote.CreateNoteRequest? Request { get; set; }
            }
            """);
        var finding = $"{probe}(4,1): error UMP020101: Probe in Host";

        var build = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Contain(finding));

        workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireUpdateBaseline=true")
            .EnsureSuccess();
        var baseline = Path.Combine(workspace.Src, "umpire-baseline.txt");
        Assert.That(File.ReadAllLines(baseline), Has.One.StartsWith("UMP020101|App/Host/Probe.cs|Probe in Host"));

        workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();

        var accepted = File.ReadAllText(baseline);
        File.Delete(baseline);
        var withoutBaseline = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false");
        Assert.That(withoutBaseline.Succeeded, Is.False, "A delete of the baseline skips the check.");
        Assert.That(withoutBaseline.Output, Does.Contain(finding));
        File.WriteAllText(baseline, accepted);

        File.Delete(probe);
        var fixedBuild = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
        Assert.That(fixedBuild.Output, Does.Contain($"{baseline}(1,1): warning UMP030501"));
    }

    static void AddBuildHook(string root, string project) {
        var path = Path.Combine(root, project);
        var text = File.ReadAllText(path).Replace("</Project>", """
              <ItemGroup>
                <PackageReference Include="Umpire.Engine.Build" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(path, text);
    }
}
