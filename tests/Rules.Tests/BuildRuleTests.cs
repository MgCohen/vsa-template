using System.IO.Compression;
using Tests.Shared;

namespace Rules.Tests;

// The build rules of VSA.Sdk, in a real build from the packed packages: VSA0001 and VSA0004 to VSA0006 run in MSBuild, before the compile.
// The built fixture tests/Fixtures/Built/Shop is one module with one slice. Each test adds one problem, builds, and removes it.
[NonParallelizable]
public sealed class BuildRuleTests {
    Workspace workspace = null!;

    string Module => Path.Combine(workspace.Src, "Modules", "Shop");

    [OneTimeSetUp]
    public void Create() {
        workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Shop"));
        Build().EnsureSuccess();
    }

    [OneTimeTearDown]
    public void Delete() => workspace.Dispose();

    DotnetResult Build(string project = "Modules/Shop/Shop.csproj") => workspace.Dotnet("build", project, "-nodeReuse:false");

    // Writes the files, builds, and then deletes the files, also when the test fails.
    DotnetResult BuildWith(params (string Path, string Text)[] files) {
        foreach (var (path, text) in files) {
            var full = Path.Combine(Module, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }
        try {
            return Build();
        } finally {
            foreach (var (path, _) in files) {
                File.Delete(Path.Combine(Module, path));
            }
        }
    }

    // Changes a project file, builds, and then restores it.
    DotnetResult BuildWithProject(string path, string text) {
        var full = Path.Combine(Module, path);
        var before = File.ReadAllText(full);
        File.WriteAllText(full, text);
        try {
            return Build();
        } finally {
            File.WriteAllText(full, before);
        }
    }

    static void Fails(DotnetResult result, string code, string text) {
        Assert.That(result.Succeeded, Is.False, $"The build passed. It must fail with {code}.");
        Assert.That(result.Output, Does.Contain($"error {code}").And.Contain(text), result.Output);
    }

    // The router task runs in the MSBuild of the consumer, also in Visual Studio MSBuild (.NET Framework), which does not
    // give Roslyn to a task. So the package must hold Roslyn next to the task DLL, and must not hold the MSBuild assemblies.
    [Test]
    public void The_SDK_package_holds_the_task_and_Roslyn() {
        using var package = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"VSA.Sdk.{LocalFeed.Version}.nupkg"));
        var tasks = package.Entries.Select(e => e.FullName).Where(n => n.StartsWith("Sdk/tasks/", StringComparison.Ordinal)).ToList();

        Assert.That(tasks, Is.SupersetOf(new[] {
            "Sdk/tasks/VSA.Sdk.Tasks.dll", "Sdk/tasks/Microsoft.CodeAnalysis.dll", "Sdk/tasks/Microsoft.CodeAnalysis.CSharp.dll" }));
        Assert.That(tasks, Has.None.Contains("Microsoft.Build"));
    }

    [Test]
    public void Each_slice_file_goes_to_its_project() {
        var bin = Path.Combine(Module, "Projects", "bin~");
        Assert.That(Directory.EnumerateFiles(bin, "Shop.Contracts.Requests.dll", SearchOption.AllDirectories), Is.Not.Empty);
        Assert.That(Directory.EnumerateFiles(bin, "Shop.Handlers.Requests.dll", SearchOption.AllDirectories), Is.Not.Empty);
    }

    // An IDE design-time build can start at Compile. The .NET SDK then adds the transitive references before the checks run.
    [Test]
    public void A_design_time_build_that_starts_at_Compile_passes() =>
        workspace.Dotnet("msbuild", "Modules/Shop/Projects/Shop.Handlers.Requests.csproj", "-t:Compile", "-p:DesignTimeBuild=true",
            "-p:BuildingProject=false", "-p:SkipCompilerExecution=true", "-p:ProvideCommandLineArgs=true", "-nodeReuse:false").EnsureSuccess();

    // The same project as a rule reference, but written by hand, so it has no VsaRule mark. A reference to the root would make a cycle.
    [Test]
    public void VSA0001_a_reference_written_by_hand() => Fails(
        BuildWithProject("Projects/Shop.Handlers.Requests.csproj", """
            <Project Sdk="VSA.Sdk">
              <ItemGroup><ProjectReference Include="Shop.Contracts.Requests.csproj" /></ItemGroup>
            </Project>
            """),
        "VSA0001", "Remove these references: Shop.Contracts.Requests.csproj");

    [Test]
    public void VSA0004_a_project_name_that_is_not_module_and_layer() => Fails(
        BuildWith(("Projects/Prices.csproj", "<Project Sdk=\"VSA.Sdk\" />\n")),
        "VSA0004", "Prices is in Shop/Projects/");

    [Test]
    public void VSA0005_a_slice_with_no_handler() => Fails(
        BuildWith(("Slices/Discounts/DiscountRules.cs", "namespace Shop.Slices.Discounts;\n\nstatic class DiscountRules { }\n")),
        "VSA0005", "Slice Discounts has no handler");

    [Test]
    public void VSA0005_a_contract_that_uses_a_handler() => Fails(
        BuildWith(("Slices/GetPrice/GetPriceAudit.cs",
            "namespace Shop.Slices.GetPrice;\n\npublic sealed record GetPriceAudit(GetPriceHandler Handler) : VSA.Contracts.Requests.IResponse;\n")),
        "VSA0005", "GetPriceAudit is a contract type, but it uses the handler GetPriceHandler");

    [Test]
    public void VSA0006_a_compile_item_written_by_hand() => Fails(
        BuildWithProject("Projects/Shop.Contracts.Requests.csproj", """
            <Project Sdk="VSA.Sdk">
              <ItemGroup><Compile Include="../ShopModule.cs" /></ItemGroup>
            </Project>
            """),
        "VSA0006", "has Compile items written by hand");

    [Test]
    public void VSA0006_a_file_that_no_project_compiles() => Fails(
        BuildWith(("Misc/Stray.cs", "namespace Shop.Misc;\n\npublic sealed class Stray { }\n")),
        "VSA0006", "Misc/Stray.cs");

    // Shop has no Domain and no Domain.Shared project. The message gives the path in the module, one time.
    [Test]
    public void VSA0006_a_published_domain_file_with_no_Domain_Shared_project() {
        var result = BuildWith(("Domain/Shared/PriceKey.cs", "namespace Shop.Domain.Shared;\n\npublic sealed record PriceKey(string Name);\n"));
        Fails(result, "VSA0006", "compiles these files: Domain/Shared/PriceKey.cs.");
    }

    [Test]
    public void VSA1003_a_public_helper_in_a_contract_file() => Fails(
        BuildWith(("Slices/GetPrice/PriceRules.cs",
            "namespace Shop.Slices.GetPrice;\n\npublic sealed record PriceNote(string Text) : VSA.Contracts.Requests.IResponse;\npublic static class PriceRules { public static bool Valid(decimal a) => a > 0; }\n")),
        "VSA1003", "PriceRules is in Shop.Contracts.Requests");
}
