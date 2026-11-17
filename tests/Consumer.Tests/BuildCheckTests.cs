using System.Text.RegularExpressions;
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
        var finding = Regex.Escape(probe) + @"\(4,\d+\): error UMP020101: Probe in Host";

        var build = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Match(finding));

        var warned = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheckMode=Warn")
            .EnsureSuccess();
        Assert.That(warned.Output, Does.Match(Regex.Escape(probe) + @"\(4,\d+\): warning UMP020101: Probe in Host"));

        File.Delete(probe);
        workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
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
