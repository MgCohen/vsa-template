using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class DefinitionBuildTests {
    const string OneLine = "<Project Sdk=\"Umpire.Sdk\" />";
    const string Failing = """
        namespace Shop.Definition;

        using Umpire.Architecture;
        using Umpire.Architecture.Blocks;
        using Umpire.Vsa.Definition;

        public sealed class ShopArchitecture : VsaArchitecture {
            protected override void Rules(RuleBook rules) => rules.For<App>(Depth.Self).HasTypes<object>().AtLeast(9);
        }
        """;
    const string Passing = """
        namespace Shop.Definition;

        using Umpire.Vsa.Definition;

        public sealed class ShopArchitecture : VsaArchitecture;
        """;
    Workspace workspace = null!;

    [SetUp]
    public void Create() {
        workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Shop"));
        File.Delete(Path.Combine(workspace.Root, "Directory.Build.props"));
    }

    [TearDown]
    public void Delete() => workspace.Dispose();

    [Test]
    public void A_changed_definition_runs_the_check_again_and_gives_its_finding() {
        Build().EnsureSuccess();

        Write("Definition/ShopArchitecture.cs", Failing);

        Assert.That(Build().Output, Does.Contain("error UMP020301"));
    }

    [TestCase("<TargetFramework>net8.0</TargetFramework>", "Exe")]
    [TestCase("<TargetFramework>netstandard2.1</TargetFramework>", "Library")]
    [TestCase("<TargetFrameworks>net8.0;net10.0</TargetFrameworks><TargetFramework />", "Exe")]
    public void A_root_with_another_target_builds_its_definition(string target, string output) {
        WriteRoot($"{target}<OutputType>{output}</OutputType>");
        if (output == "Library") {
            File.Delete(Path.Combine(workspace.Src, "App", "Program.cs"));
        }

        var build = Build();

        Assert.That(build.Succeeded, Is.True, build.Output);
        Assert.That(Definitions("Shop.Definition.dll"), Is.Not.Empty);
    }

    [Test]
    public void A_removed_definition_project_leaves_the_output() {
        Build().EnsureSuccess();
        DeleteDefinition();

        Build().EnsureSuccess();

        Assert.That(Definitions("Shop.Definition.*"), Is.Empty);
    }

    [Test]
    public void A_definition_with_an_embedded_pdb_leaves_no_stale_pdb() {
        Build().EnsureSuccess();
        Write("Definition/Shop.Definition.csproj",
            "<Project Sdk=\"Umpire.Sdk\"><PropertyGroup><DebugType>embedded</DebugType></PropertyGroup></Project>");

        Build().EnsureSuccess();

        Assert.That(Definitions("Shop.Definition.pdb"), Is.Empty);
        Assert.That(Definitions("Shop.Definition.dll"), Is.Not.Empty);
    }

    [Test]
    public void A_publish_copies_no_definition() {
        var folder = Path.Combine(workspace.Root, "published");

        workspace.Dotnet("publish", "Shop.csproj", "-o", folder, "-nodeReuse:false").EnsureSuccess();

        Assert.That(Directory.GetFiles(folder, "*Definition*", SearchOption.AllDirectories), Is.Empty);
        Assert.That(Directory.GetFiles(folder, "Shop.dll"), Is.Not.Empty);
    }

    [Test]
    public void A_build_with_an_output_folder_keeps_the_definition_out_of_the_app() {
        var folder = Path.Combine(workspace.Root, "out");

        workspace.Dotnet("build", "Shop.csproj", "-o", folder, "-nodeReuse:false").EnsureSuccess();

        Assert.That(File.Exists(Path.Combine(folder, "Shop.Definition.dll")), Is.False);
        Assert.That(File.Exists(Path.Combine(folder, "umpire-definitions", "Shop.Definition.dll")), Is.True);
    }

    [Test]
    public void A_build_with_one_output_folder_keeps_the_definition_of_each_project() {
        var folder = Path.Combine(workspace.Root, "out");
        WriteTools();
        Write("Plain/Plain.csproj", OneLine);
        Write("Shop.csproj", """
            <Project Sdk="Umpire.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="Tools/Tools.csproj" />
                <ProjectReference Include="Plain/Plain.csproj" />
              </ItemGroup>
            </Project>
            """);

        var build = workspace.Dotnet("build", "Shop.csproj", "-o", folder, "-nodeReuse:false", "-p:UmpireCheck=false", "-m:1");

        Assert.That(build.Succeeded, Is.True, build.Output);
        Assert.That(File.Exists(Path.Combine(folder, "umpire-definitions", "Tools.Definition.dll")), Is.True);
        Assert.That(File.Exists(Path.Combine(folder, "umpire-definitions", "Shop.Definition.dll")), Is.True);
    }

    [Test]
    public void The_definition_below_a_nested_test_project_is_a_definition_of_the_outer_project() {
        Write("App/Packs/X/X.Tests.csproj", OneLine);
        Write("App/Packs/X/Definition/X.Definition.csproj", OneLine);
        Write("App/Packs/X/Definition/XArchitecture.cs", Passing.Replace("Shop", "X"));

        workspace.Dotnet("build", "Shop.csproj", "-nodeReuse:false", "-p:UmpireCheck=false").EnsureSuccess();

        Assert.That(Definitions("X.Definition.dll"), Is.Not.Empty);
    }

    [Test]
    public void An_ignored_definition_folder_gives_no_UMP030512() {
        DeleteDefinition();
        Write("Definition/Draft.cs", "class Draft;");
        Write(".umpireconfig", "umpire.check.ignored_folders = \"Definition\"\n");

        Assert.That(Build().Output, Does.Not.Contain("UMP030512").And.Not.Contain("UMP030308"));
    }

    [Test]
    public void A_definition_derives_from_a_definition_that_the_app_does_not_reference() {
        Write("company/Company.csproj", OneLine);
        Write("company/CompanyArchitecture.cs", Failing.Replace("Shop.Definition", "Company")
            .Replace("sealed class ShopArchitecture", "class CompanyArchitecture"));
        Write("Definition/Shop.Definition.csproj", """
            <Project Sdk="Umpire.Sdk">
              <ItemGroup>
                <ProjectReference Include="../company/Company.csproj" />
              </ItemGroup>
            </Project>
            """);
        Write("Definition/ShopArchitecture.cs", """
            namespace Shop.Definition;

            using Company;

            public sealed class ShopArchitecture : CompanyArchitecture;
            """);

        var build = Build();

        Assert.That(build.Output, Does.Contain("error UMP020301"));
        Assert.That(Definitions("Company.dll"), Is.Not.Empty);
        Assert.That(File.Exists(Path.Combine(workspace.Src, "artifacts~", "bin", "Shop", "debug", "Company.dll")), Is.False);
    }

    [Test]
    public void The_definition_of_a_nested_root_project_is_not_a_definition_of_the_outer_project() {
        WriteTools();

        Build().EnsureSuccess();

        Assert.That(Definitions("Tools.Definition.dll"), Is.Empty);
    }

    [Test]
    public void The_definitions_of_a_referenced_project_are_in_the_output() {
        WriteTools();
        Write("Shop.csproj", """
            <Project Sdk="Umpire.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="Tools/Tools.csproj" />
              </ItemGroup>
            </Project>
            """);

        var build = workspace.Dotnet("build", "Shop.csproj", "-nodeReuse:false");

        Assert.That(build.Output, Does.Not.Contain("UMP030511").And.Not.Contain("UMP030512"));
        Assert.That(Definitions("Tools.Definition.dll").Where(file => file.Contains("Shop")), Is.Not.Empty);
    }

    [TestCase("App/ShopArchitecture.cs", "error UMP030511")]
    [TestCase("App/Packs/X/Definition/ShopArchitecture.cs", "error UMP030511")]
    [TestCase("Definition/ShopArchitecture.cs", "warning UMP030308")]
    public void A_definition_with_no_definition_project_gives_a_finding(string file, string finding) {
        DeleteDefinition();
        Write(file, Failing);

        Assert.That(Build().Output, Does.Contain(finding));
    }

    [Test]
    public void A_definition_in_a_lower_case_folder_is_not_ignored() {
        DeleteDefinition();
        Write("definition/Shop.Definition.csproj", OneLine);
        Write("definition/ShopArchitecture.cs", Failing);

        Assert.That(Build().Output, Does.Contain("error UMP020301"));
    }

    [Test]
    public void A_definition_that_is_not_in_the_bin_gives_UMP030512() {
        Write("Directory.Build.targets", """
            <Project>
              <Target Name="NoDefinition" AfterTargets="_UmpireCopyDefinitions">
                <RemoveDir Directories="$(TargetDir)umpire-definitions" />
              </Target>
            </Project>
            """);

        Assert.That(Build().Output, Does.Contain("error UMP030512"));
    }

    DotnetResult Build() => workspace.Dotnet("build", "Shop.csproj", "-nodeReuse:false");

    IReadOnlyList<string> Definitions(string pattern) =>
        [.. Directory.GetFiles(Path.Combine(workspace.Src, "artifacts~", "bin"), pattern, SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(Path.GetDirectoryName(file)) == "umpire-definitions")];

    void DeleteDefinition() => Directory.Delete(Path.Combine(workspace.Src, "Definition"), recursive: true);

    void WriteRoot(string properties) =>
        Write("Shop.csproj", $"<Project Sdk=\"Umpire.Sdk\"><PropertyGroup>{properties}</PropertyGroup></Project>");

    void WriteTools() {
        Write("Tools/Tools.csproj", OneLine);
        Write("Tools/Definition/Tools.Definition.csproj", OneLine);
        Write("Tools/Definition/ToolsArchitecture.cs", Passing.Replace("Shop", "Tools"));
    }

    void Write(string path, string content) {
        var file = Path.Combine(workspace.Src, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
