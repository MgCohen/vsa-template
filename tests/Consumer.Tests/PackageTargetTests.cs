using System.IO.Compression;
using System.Text.Json.Nodes;
using Tests.Shared;

namespace Consumer.Tests;

[Category("Package")]
public sealed class PackageTargetTests {
    [TestCase("Umpire.Web", "lib/net9.0/Umpire.Web.dll")]
    [TestCase("Umpire.Console", "lib/net8.0/Umpire.Console.dll")]
    [TestCase("Umpire.Testing", "lib/net8.0/Umpire.Testing.dll")]
    [TestCase("Umpire.Cli", "tools/net8.0/any/Umpire.Cli.dll")]
    public void A_dotnet_package_has_only_the_library_of_its_lowest_framework(string package, string library) {
        using var nupkg = Open(package);
        var libraries = nupkg.Entries.Select(e => e.FullName)
            .Where(name => name.EndsWith($"/{Path.GetFileName(library)}", StringComparison.Ordinal))
            .ToList();

        Assert.That(libraries, Is.EqualTo(new[] { library }),
            "Each package targets the lowest framework that its code needs. See \"Runtime targets\" in docs/requirements.md.");
    }

    [Test]
    public void The_tool_runs_on_each_newer_runtime() {
        using var nupkg = Open("Umpire.Cli");
        using var config = nupkg.GetEntry("tools/net8.0/any/Umpire.Cli.runtimeconfig.json")!.Open();
        var options = JsonNode.Parse(config)!["runtimeOptions"]!;

        Assert.That(options["rollForward"]?.GetValue<string>(), Is.EqualTo("Major"));
    }

    static ZipArchive Open(string package) =>
        ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
}
