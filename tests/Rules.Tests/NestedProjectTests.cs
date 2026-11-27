using Tests.Shared;

namespace Rules.Tests;

public sealed class NestedProjectTests {
    [TestCase(10)]
    [TestCase(110)]
    public void The_root_project_does_not_compile_the_files_of_a_nested_project(int files) {
        var root = TempFolder.Create();
        try {
            WriteApp(root, files);

            var result = Dotnet.Run(root, null, "msbuild", "Root.csproj", "-getItem:Compile", "-nodeReuse:false").EnsureSuccess();

            Assert.That(result.Output, Does.Contain($"Part{files}.cs").And.Not.Contain("AppTests.cs"), result.Output);
        } finally {
            TempFolder.Delete(root);
        }
    }

    static void WriteApp(string root, int files) {
        var targets = Repo.Path("tools", "bundle", "Umpire", "build", "Umpire.After.targets");
        File.WriteAllText(Path.Combine(root, "Root.csproj"), $"""
            <Project>
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
              <Import Project="{targets}" />
            </Project>
            """);
        Directory.CreateDirectory(Path.Combine(root, "Tests"));
        File.WriteAllText(Path.Combine(root, "Tests", "Tests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(root, "Tests", "AppTests.cs"), "internal static class AppTests;");
        Directory.CreateDirectory(Path.Combine(root, "App"));
        foreach (var number in Enumerable.Range(1, files)) {
            File.WriteAllText(Path.Combine(root, "App", $"Part{number}.cs"), $"internal static class Part{number};");
        }
    }
}
