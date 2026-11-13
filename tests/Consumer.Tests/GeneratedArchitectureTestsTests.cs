using Tests.Shared;

namespace Consumer.Tests;

public sealed class GeneratedArchitectureTestsTests {
    [Test]
    public void The_generated_tests_run_in_a_project_with_no_test_of_its_own() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "GeneratedTests"));

        var result = workspace.Dotnet("test", "--project", "Tests");

        Assert.That(result.Output, Does.Not.Contain("Zero tests ran"));
        result.EnsureSuccess();
        Assert.That(result.Output, Does.Contain("succeeded: "));
    }
}
