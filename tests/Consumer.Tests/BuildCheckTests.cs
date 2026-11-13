using Tests.Shared;

namespace Consumer.Tests;

// Installs the build hook package (VSA.Architecture.Build) on the host and on the tests of a consumer copy of samples/Notes.
// The build of the host runs the rule engine, and ArchitectureTests runs the same engine, with the same findings.
public sealed class BuildCheckTests {
    const string Summary = "vsa-engine: ";

    [Test]
    [CancelAfter(20 * 60 * 1000)]
    public void The_build_and_the_tests_run_the_rule_engine(CancellationToken ct) {
        using var workspace = Workspace.Create(Repo.Path("samples", "Notes"));
        NotesConsumerTests.UsePackages(workspace.Src);
        AddBuildHook(workspace.Src, "App/Host/App.Host.csproj");
        AddBuildHook(workspace.Src, "Tests/Notes.Tests.csproj");

        var clean = workspace.Dotnet("build", "App/Host", "-nodeReuse:false").EnsureSuccess();
        Assert.That(clean.Output, Does.Contain(Summary).And.Contain("0 errors").And.Contain("HELLO: Hello from a plugin"));

        var again = workspace.Dotnet("build", "App/Host", "--no-restore", "-nodeReuse:false").EnsureSuccess();
        Assert.That(again.Output, Does.Not.Contain(Summary), "A build with no change runs the check again.");

        workspace.Dotnet("build", "Tests", "-nodeReuse:false").EnsureSuccess();
        workspace.Dotnet("test", "--project", "Tests", "--no-build").EnsureSuccess();

        var probe = Path.Combine(workspace.Src, "App", "Host", "Probe.cs");
        File.WriteAllText(probe, """
            public static class Probe {
                public static Notes.Slices.CreateNote.CreateNoteRequest? Request { get; set; }
            }
            """);
        var finding = $"{probe}(2,1): error VSA2001: Probe in Host";

        var build = workspace.Dotnet("build", "App/Host", "--no-restore", "-nodeReuse:false");
        Assert.That(build.Succeeded, Is.False, build.Output);
        Assert.That(build.Output, Does.Contain(finding));

        workspace.Dotnet("build", "Tests", "--no-restore", "-p:VsaCheck=false", "-nodeReuse:false").EnsureSuccess();
        var test = workspace.Dotnet("test", "--project", "Tests", "--no-build");
        Assert.That(test.Succeeded, Is.False, test.Output);
        Assert.That(test.Output, Does.Contain(finding));
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
