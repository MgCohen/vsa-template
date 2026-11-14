using Tests.Shared;

namespace Consumer.Tests;

public sealed class BuildCheckTests {
    const string Summary = "vsa-engine: ";

    [Test]
    [CancelAfter(20 * 60 * 1000)]
    public void The_build_of_the_host_runs_the_rule_engine(CancellationToken ct) {
        using var workspace = Workspace.Create(Repo.Path("samples", "Notes"));
        NotesConsumerTests.UsePackages(workspace.Src);
        AddBuildHook(workspace.Src, "Notes.csproj");

        var clean = workspace.Dotnet("build", "Notes.csproj", "-nodeReuse:false").EnsureSuccess();
        Assert.That(clean.Output, Does.Contain(Summary).And.Contain("0 errors").And.Contain("HELLO: Hello from a plugin"));

        var again = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false").EnsureSuccess();
        Assert.That(again.Output, Does.Not.Contain(Summary), "A build with no change runs the check again.");

        var probe = Path.Combine(workspace.Src, "App", "Host", "Probe.cs");
        File.WriteAllText(probe, """
            namespace App.Host;

            public static class Probe {
                public static Notes.Slices.CreateNote.CreateNoteRequest? Request { get; set; }
            }
            """);
        var finding = $"{probe}(4,1): error VSA2001: Probe in Host";

        var build = workspace.Dotnet("build", "Notes.csproj", "--no-restore", "-nodeReuse:false");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Contain(finding));
    }

    static void AddBuildHook(string root, string project) {
        var path = Path.Combine(root, project);
        var text = File.ReadAllText(path).Replace("</Project>", """
              <ItemGroup>
                <PackageReference Include="VSA.Architecture.Build" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(path, text);
    }
}
