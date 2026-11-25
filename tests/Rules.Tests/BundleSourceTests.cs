using System.Text.Json;
using Tests.Shared;

namespace Rules.Tests;

public sealed class BundleSourceTests {
    static readonly string[] RuntimeProjects = [
        "architecture/Markers/Umpire.Markers.csproj",
        "architecture/Umpire.Architecture.csproj",
        "framework/Components/Config/Umpire.Config.csproj",
        "framework/Components/Messages/Contracts/Umpire.Messages.Contracts.csproj",
        "framework/Components/Messages/Handlers/Umpire.Messages.Handlers.csproj",
        "framework/Kernel/Umpire.Kernels.csproj",
        "framework/Kernel/Definition/Umpire.Kernels.Definition.csproj",
        "packs/Vsa/Definition/Umpire.Vsa.csproj",
        "packs/Vsa/Kernel/Composition/Umpire.Vsa.Composition.csproj",
        "packs/Vsa/Kernel/Domain/Umpire.Vsa.Domain.csproj",
        "packs/Vsa/Kernel/Infrastructure/Umpire.Vsa.Infrastructure.csproj",
        "packs/Vsa/Kernel/Presenters/Umpire.Vsa.Presenters.csproj",
        "packs/Vsa/Kernel/Contracts/Requests/Umpire.Vsa.Contracts.Requests.csproj",
        "packs/Vsa/Kernel/Contracts/Events/Umpire.Vsa.Contracts.Events.csproj",
        "packs/Vsa/Kernel/Contracts/Commands/Umpire.Vsa.Contracts.Commands.csproj",
        "packs/Vsa/Kernel/Handlers/Requests/Umpire.Vsa.Handlers.Requests.csproj",
        "packs/Vsa/Kernel/Handlers/Events/Umpire.Vsa.Handlers.Events.csproj",
        "packs/Vsa/Kernel/Handlers/Commands/Umpire.Vsa.Handlers.Commands.csproj",
    ];

    static readonly string[] WebProjects = [
        "framework/Kernel/Hosts/Web/Umpire.Hosts.Web.csproj",
        "packs/Vsa/Kernel/Views/Umpire.Vsa.Views.csproj",
    ];

    static IEnumerable<TestCaseData> Bundles() {
        yield return new TestCaseData("tools/bundle/Umpire/Umpire.csproj", RuntimeProjects).SetArgDisplayNames("Umpire");
        yield return new TestCaseData("tools/bundle/Web/Umpire.Web.csproj", WebProjects).SetArgDisplayNames("Umpire.Web");
        yield return new TestCaseData("tools/bundle/Console/Umpire.Console.csproj",
            new[] { "framework/Kernel/Hosts/Console/Umpire.Hosts.Console.csproj" }).SetArgDisplayNames("Umpire.Console");
        yield return new TestCaseData("tools/bundle/Testing/Umpire.Testing.csproj",
            new[] { "packs/Vsa/Tests/Unit/Umpire.Vsa.Testing.Unit.csproj" }).SetArgDisplayNames("Umpire.Testing");
    }

    [TestCaseSource(nameof(Bundles))]
    public void A_bundle_compiles_the_files_of_its_runtime_projects(string bundle, string[] projects) {
        var expected = projects.SelectMany(SourceFiles).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.That(SourceFiles(bundle).Order(StringComparer.OrdinalIgnoreCase),
            Is.EqualTo(expected.Order(StringComparer.OrdinalIgnoreCase)));
    }

    static IEnumerable<string> SourceFiles(string project) {
        var result = Dotnet.Run(Repo.Root, null, "msbuild", Repo.Path(project), "-getItem:Compile", "-p:TargetFramework=net10.0",
            "-nodeReuse:false").EnsureSuccess();
        var json = JsonDocument.Parse(result.Output[result.Output.IndexOf('{')..]);
        return json.RootElement.GetProperty("Items").GetProperty("Compile").EnumerateArray()
            .Select(item => Path.GetFullPath(item.GetProperty("FullPath").GetString()!))
            .ToList();
    }
}
