using Tests.Shared;
using Umpire.Cli;
using Umpire.Cli.Commands;
using Umpire.Cli.Scaffolding;
using Umpire.Cli.Scaffolding.Setup;

namespace Cli.Tests;

public sealed class SetupTests {
    const string Project = "Shop.csproj";
    const string BuildProps = "Directory.Build.props";
    const string WebProject = "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n";
    static readonly string V = ToolVersion.Current;
    static string WebReferences => $"  <ItemGroup>\n    <PackageReference Include=\"Umpire.Web\" Version=\"{V}\" />\n"
        + $"    <PackageReference Include=\"Umpire.Engine\" Version=\"{V}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n";
    string parent = "";
    string block = "";

    static IEnumerable<string> TaskIds => SetupTasks.Default.Select(task => task.Id);

    [SetUp]
    public void SetUp() {
        parent = TempFolder.Create();
        block = Path.Combine(parent, "Shop");
        Directory.CreateDirectory(block);
        Write(Project, WebProject);
    }

    [TearDown]
    public void TearDown() => TempFolder.Delete(parent);

    [Test]
    public void AFolderWithOnlyAProjectGetsEachSetupFile() {
        var plan = Plan();

        var changed = plan.Tasks.Where(task => task.Id is not "umpire-config" and not "global-json");
        Assert.That(changed.Select(task => task.Result),
            Has.All.TypeOf<SetupResult.Change>());
        Assert.That(plan.Files.Select(file => file.Path), Is.EquivalentTo(new[] { "nuget.config", ".gitignore", BuildProps, Project }));
        foreach (var path in new[] { "nuget.config", ".gitignore", BuildProps }) {
            Assert.That(plan.Files.Single(file => file.Path == path).Content, Is.EqualTo(Templates.SetupFile(path, V)), path);
        }
    }

    [TestCaseSource(nameof(TaskIds))]
    public void ASecondRunOfEachTaskChangesNothing(string id) {
        Plan().Write();

        Assert.That(Result(id), Is.TypeOf<SetupResult.Done>());
    }

    [Test]
    public void ACompleteSetupHasNoFileToWrite() {
        Plan().Write();

        var plan = Plan();
        Assert.That(plan.IsComplete, Is.True);
        Assert.That(plan.Files, Is.Empty);
    }

    [Test]
    public void AGlobalJsonWithNoUmpirePinIsDone() {
        Write("global.json", "{\n  \"sdk\": {\n    \"version\": \"9.0.100\"\n  }\n}\n");

        Assert.That(Result("global-json"), Is.TypeOf<SetupResult.Done>());
    }

    [Test]
    public void AnUmpirePinInGlobalJsonIsByHand() {
        Write("global.json", "{ \"msbuild-sdks\": { \"Umpire.Sdk\": \"0.0.1\" } }");

        Assert.That(Result("global-json"), Is.TypeOf<SetupResult.ByHand>().With.Property("Message").Contain("Remove the entry"));
    }

    [Test]
    public void AGlobalJsonWithACommentIsByHand() {
        Write("global.json", "{\n  // the SDK\n  \"sdk\": { \"version\": \"10.0.100\" }\n}\n");

        Assert.That(Result("global-json"), Is.TypeOf<SetupResult.ByHand>());
    }

    [Test]
    public void ANugetConfigKeepsItsSourcesAndGetsTheUmpireFeed() {
        Write("nuget.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <add key=\"company\" value=\"https://example.com/index.json\" />\n  </packageSources>\n</configuration>\n");

        var config = Written("nuget.config");

        Assert.That(config, Does.Contain("    <add key=\"company\" value=\"https://example.com/index.json\" />\n    <add key=\"umpire\" value=\"https://nuget.pkg.github.com/MgCohen/index.json\" />\n  </packageSources>"));
        Assert.That(config, Does.Contain("<packageSourceCredentials>"));
        Assert.That(config, Does.Contain("%GITHUB_TOKEN%"));
        Assert.That(config, Does.Not.Contain("<clear />"));
        Assert.That(System.Xml.Linq.XDocument.Parse(config).Descendants("add").Count(), Is.EqualTo(4));
    }

    [Test]
    public void ASourceReplacesTheFeedInAnExistingNugetConfig() {
        Write("nuget.config", "<configuration>\n  <packageSources>\n  </packageSources>\n</configuration>\n");

        Plan(sources: "feed").Write();
        var config = File.ReadAllText(Path.Combine(block, "nuget.config"));

        Assert.That(config, Does.Contain($"<add key=\"umpire\" value=\"{Path.GetFullPath("feed")}\" />"));
        Assert.That(config, Does.Not.Contain("packageSourceCredentials"));
    }

    [Test]
    public void AGitIgnoreGetsOnlyTheMissingLines() {
        Write(".gitignore", "bin/\nnode_modules/");

        Assert.That(Written(".gitignore"), Is.EqualTo("bin/\nnode_modules/\nartifacts~/\nobj/\nTestResults/\n*.user\n.vs/\n"));
    }

    [Test]
    public void AFileWithCrLfKeepsCrLf() {
        Write(".gitignore", "bin/\r\nobj/\r\n");
        Write(Project, WebProject.Replace("\n", "\r\n"));

        Plan().Write();

        Assert.That(File.ReadAllText(Path.Combine(block, ".gitignore")), Is.EqualTo("bin/\r\nobj/\r\nartifacts~/\r\nTestResults/\r\n*.user\r\n.vs/\r\n"));
        Assert.That(File.ReadAllText(Path.Combine(block, Project)).Replace("\r\n", ""), Does.Not.Contain("\n"));
    }

    [Test]
    public void APlainWebProjectGetsTheWebPackageAndTheEngineWithTheVersion() =>
        Assert.That(Written(Project), Is.EqualTo(WebProject.Replace("</Project>\n", WebReferences + "</Project>\n")));

    [Test]
    public void APlainProjectGetsTheVsaPackageAndTheEngine() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        Assert.That(Written(Project), Is.EqualTo("<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n"
            + $"    <PackageReference Include=\"Umpire.Vsa\" Version=\"{V}\" />\n"
            + $"    <PackageReference Include=\"Umpire.Engine\" Version=\"{V}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n"));
    }

    [Test]
    public void AProjectWithTheUmpirePackagesIsDone() {
        Write(Project, WebProject.Replace("</Project>\n", WebReferences + "</Project>\n"));

        Assert.That(Result("umpire-packages"), Is.TypeOf<SetupResult.Done>());
    }

    [Test]
    public void AConsoleProjectGetsTheVsaPackageAndTheEngine() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Umpire.Console\" Version=\"0.0.1\" />\n  </ItemGroup>\n</Project>\n");

        var written = Written(Project);

        Assert.That(written, Does.Contain("Include=\"Umpire.Engine\"").And.Contain("Include=\"Umpire.Vsa\"").And.Not.Contain("Include=\"Umpire.Web\""));
        Assert.That(written, Does.Contain("Include=\"Umpire.Console\" Version=\"0.0.1\""));
    }

    [Test]
    public void AProjectWithCentralVersionsGetsVersionOverride() {
        Write("Directory.Packages.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");

        Assert.That(Written(Project), Does.Contain($"<PackageReference Include=\"Umpire.Web\" VersionOverride=\"{V}\" />"));
    }

    [Test]
    public void TheRemovedUmpireWebSdkBecomesTheWebSdkWithTheWebPackageAndTheGeneralSettings() {
        Write(Project, "<Project Sdk=\"Umpire.Sdk.Web/0.0.1\" />\n");

        var written = Written(Project);

        Assert.That(written, Does.StartWith("<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n"
            + "    <Nullable>enable</Nullable>\n    <ImplicitUsings>enable</ImplicitUsings>\n  </PropertyGroup>\n"));
        Assert.That(written, Does.Contain($"<PackageReference Include=\"Umpire.Web\" Version=\"{V}\" />"));
    }

    [Test]
    public void TheRemovedUmpireSdkAfterAnotherSdkBecomesTheNetSdk() {
        Write(Project, "<Project Sdk=\"Microsoft.Build.NoTargets/3.7.0;Umpire.Sdk\" />\n");

        Assert.That(Written(Project), Does.StartWith("<Project Sdk=\"Microsoft.Build.NoTargets/3.7.0;Microsoft.NET.Sdk\">\n"));
    }

    [Test]
    public void AnUmpireSdkElementIsByHand() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <Sdk Name=\"Umpire.Sdk\" />\n</Project>\n");

        Assert.That(Result("umpire-packages"), Is.TypeOf<SetupResult.ByHand>().With.Property("Message").Contain("Remove the <Sdk> element"));
    }

    [Test]
    public void AProjectWithSingleQuotesKeepsItsSdk() {
        Write(Project, "<Project Sdk='Umpire.Sdk'>\n</Project>\n");

        Assert.That(Written(Project), Does.StartWith("<Project Sdk='Microsoft.NET.Sdk'>\n"));
    }

    [Test]
    public void AProjectWithSpacesAroundTheSdkAttributeGetsTheNetSdk() {
        Write(Project, "<Project Sdk = \"Umpire.Sdk.Web\" />\n");

        Assert.That(Written(Project), Does.StartWith("<Project Sdk = \"Microsoft.NET.Sdk.Web\">\n"));
    }

    [Test]
    public void AProjectWithNoSdkAttributeIsByHand() {
        Write(Project, "<Project>\n</Project>\n");

        Assert.That(Result("umpire-packages"), Is.TypeOf<SetupResult.ByHand>().With.Property("Message").Contain("has no Sdk attribute"));
    }

    [Test]
    public void AProjectWithCentralVersionsInAParentFolderGetsVersionOverride() {
        File.WriteAllText(Path.Combine(parent, "Directory.Build.props"), "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>\n");

        Assert.That(Written(Project), Does.Contain($"<PackageReference Include=\"Umpire.Engine\" VersionOverride=\"{V}\" PrivateAssets=\"all\" />"));
    }

    [Test]
    public void AnExistingBuildPropsIsDone() {
        Write(BuildProps, "<Project />\n");

        Assert.That(Result("build-props"), Is.TypeOf<SetupResult.Done>());
    }

    [Test]
    public void ANewBuildPropsImportsTheParentBuildPropsIfOneExists() {
        var written = Written(BuildProps);

        Assert.That(written, Does.Contain("GetPathOfFileAbove('Directory.Build.props'"));
        Assert.That(written, Does.Contain("<UseArtifactsOutput>true</UseArtifactsOutput>"));
    }

    [Test]
    public void ABlockWithASecondProjectGetsAnUmpireconfigFile() {
        Write("Tests/Shop.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        Assert.That(Written(".umpireconfig"), Is.EqualTo(Templates.SetupFile(".umpireconfig", V)));
    }

    [Test]
    public void ABlockWithOneProjectGetsNoUmpireconfigFile() {
        Plan().Write();

        Assert.That(File.Exists(Path.Combine(block, ".umpireconfig")), Is.False);
    }

    [Test]
    public void ANewNugetConfigKeepsTheParentFeeds() {
        File.WriteAllText(Path.Combine(parent, "nuget.config"), "<configuration />\n");

        Assert.That(Written("nuget.config"), Does.Not.Contain("<clear />"));
    }

    [Test]
    public void TheSetupWritesNoGlobalJson() {
        File.WriteAllText(Path.Combine(parent, "global.json"), "{ \"sdk\": { \"version\": \"10.0.200\" } }");

        Assert.That(Result("global-json"), Is.TypeOf<SetupResult.Done>());
        Assert.That(Plan().Files.Select(file => file.Path), Has.None.EqualTo("global.json"));
    }

    [Test]
    public void AProjectWithTabsKeepsItsTabs() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk\">\n\t<PropertyGroup>\n\t\t<OutputType>Exe</OutputType>\n\t</PropertyGroup>\n</Project>\n");

        Assert.That(Written(Project), Is.EqualTo(
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n\t<PropertyGroup>\n\t\t<OutputType>Exe</OutputType>\n\t</PropertyGroup>\n"
            + $"\t<ItemGroup>\n\t\t<PackageReference Include=\"Umpire.Vsa\" Version=\"{V}\" />\n"
            + $"\t\t<PackageReference Include=\"Umpire.Engine\" Version=\"{V}\" PrivateAssets=\"all\" />\n\t</ItemGroup>\n</Project>\n"));
    }

    [Test]
    public void ANugetConfigWithFourSpacesGetsFourSpaces() {
        Write("nuget.config", "<configuration>\n    <packageSources>\n        <add key=\"company\" value=\"https://example.com/index.json\" />\n    </packageSources>\n</configuration>\n");

        var config = Written("nuget.config");

        Assert.That(config, Does.Contain("\n        <add key=\"umpire\" value=\"https://nuget.pkg.github.com/MgCohen/index.json\" />\n    </packageSources>"));
        Assert.That(config, Does.Contain("\n    <packageSourceCredentials>\n        <umpire>\n            <add key=\"Username\" value=\"umpire\" />"));
    }

    [Test]
    public void AFileWithMixedLineEndingsGetsItsDominantEnding() {
        Write(".gitignore", "bin/\r\nobj/\r\nnode_modules/\n");

        Plan().Write();

        Assert.That(File.ReadAllText(Path.Combine(block, ".gitignore")),
            Is.EqualTo("bin/\r\nobj/\r\nnode_modules/\r\nartifacts~/\r\nTestResults/\r\n*.user\r\n.vs/\r\n"));
    }

    [Test]
    public void ASourceIsAddedToAConfigThatHasTheGitHubFeed() {
        Write("nuget.config", "<configuration>\n  <packageSources>\n    <add key=\"umpire\" value=\"https://nuget.pkg.github.com/MgCohen/index.json\" />\n  </packageSources>\n</configuration>\n");

        Assert.That(Result("nuget-config"), Is.TypeOf<SetupResult.Done>());
        Plan(sources: "feed").Write();

        var config = File.ReadAllText(Path.Combine(block, "nuget.config"));
        Assert.That(config, Does.Contain($"<add key=\"umpire2\" value=\"{Path.GetFullPath("feed")}\" />"));
        Assert.That(Plan(sources: "feed").Tasks.Single(task => task.Id == "nuget-config").Result, Is.TypeOf<SetupResult.Done>());
    }

    [Test]
    public void AnEditOfAMissingElementGivesNoText() {
        Assert.That(XmlText.AddAtEnd("<Other />\n", "Project", "  <A />\n"), Is.Null);
        Assert.That(XmlText.AddAtStart("<Other />\n", "Project", "  <A />\n"), Is.Null);
    }

    [Test]
    public void InitCheckShowsEachByHandMessage() {
        Write("global.json", "{ \"msbuild-sdks\": { \"Umpire.Sdk\": \"0.0.1\" } }");
        var output = new StringWriter();
        var console = Console.Out;
        Console.SetOut(output);
        try {
            RunInit("init", "--check", "true");
        } finally {
            Console.SetOut(console);
        }

        Assert.That(output.ToString(), Does.Contain("by hand: global.json pins Umpire.Sdk in"));
    }

    [Test]
    public void NewAppStopsIfASetupFileExists() {
        File.Delete(Path.Combine(block, Project));
        Write(".gitignore", "bin/\n");

        Assert.That(() => RunIn(parent, "new", "app", "Shop"),
            Throws.TypeOf<UmpireException>().With.Message.Contain(".gitignore").And.Message.Contain("umpire init"));
        Assert.That(Directory.GetFiles(block), Is.EqualTo(new[] { Path.Combine(block, ".gitignore") }));
    }

    [Test]
    public void NewAppStopsIfASetupFileExistsWithTheFullSetup() {
        File.Delete(Path.Combine(block, Project));
        Write(".gitignore", Templates.SetupFile(".gitignore", V));

        Assert.That(() => RunIn(parent, "new", "app", "Shop"),
            Throws.TypeOf<UmpireException>().With.Message.Contain(".gitignore").And.Message.Contain("umpire init"));
        Assert.That(Directory.GetFiles(block), Is.EqualTo(new[] { Path.Combine(block, ".gitignore") }));
    }

    [Test]
    public void ANewAppPlanHasNoByHandTaskAndPlansEachSetupFile() {
        var plan = NewAppCommand.Plan("Shop", [], V);

        Assert.That(plan.ByHand, Is.Empty);
        Assert.That(plan.Files.Select(file => file.Path),
            Is.SupersetOf(new[] { "nuget.config", ".gitignore", ".umpireconfig", BuildProps, Project }).And.None.EqualTo("global.json"));
    }

    [Test]
    public void NewAppStopsOnAStrayUmpireConfig() {
        File.Delete(Path.Combine(block, Project));
        Write(".umpireconfig", "");

        Assert.That(() => RunIn(parent, "new", "app", "Shop"),
            Throws.TypeOf<UmpireException>().With.Message.Contain(".umpireconfig").And.Message.Contain("umpire init"));
        Assert.That(Directory.GetFiles(block), Is.EqualTo(new[] { Path.Combine(block, ".umpireconfig") }));
    }

    [Test]
    public void ANewAppPlanReadsNoFileOnDisk() {
        Write(".gitignore", "bin/\n");

        var plan = Plan(existingApp: false);

        Assert.That(plan.Files.Single(file => file.Path == ".gitignore").Content, Is.EqualTo(Templates.SetupFile(".gitignore", V)));
    }

    [Test]
    public void NewAppDoesNotReadTheParentFiles() {
        File.WriteAllText(Path.Combine(parent, "nuget.config"), "<configuration />\n");

        Assert.That(Plan(existingApp: false).Files.Single(file => file.Path == "nuget.config").Content,
            Is.EqualTo(Templates.SetupFile("nuget.config", V)));
    }

    [Test]
    public void InitCheckWritesNothingAndFailsIfTheSetupIsNotComplete() {
        Assert.That(RunInit("init", "--check", "true"), Is.EqualTo(1));

        Assert.That(Directory.GetFiles(block), Is.EqualTo(new[] { Path.Combine(block, Project) }));
    }

    [Test]
    public void InitCheckPassesOnACompleteSetup() {
        Plan().Write();

        Assert.That(RunInit("init", "--check", "true"), Is.EqualTo(0));
    }

    [Test]
    public void InitStopsWithNoProject() {
        File.Delete(Path.Combine(block, Project));

        Assert.That(() => RunInit("init"), Throws.TypeOf<UmpireException>().With.Message.Contain("umpire new app"));
    }

    [Test]
    public void InitStopsWithTwoProjects() {
        Write("Other.csproj", WebProject);

        Assert.That(() => RunInit("init"), Throws.TypeOf<UmpireException>().With.Message.Contain("2 .csproj files"));
        Assert.That(File.Exists(Path.Combine(block, "nuget.config")), Is.False);
    }

    [Test]
    public void InitRejectsACheckValueThatIsNotABoolean() =>
        Assert.That(() => RunInit("init", "--check", "yes"), Throws.TypeOf<UmpireException>().With.Message.Contain("--check true"));

    SetupPlan Plan(bool existingApp = true, params string[] sources) =>
        SetupTasks.Plan(new SetupContext(block, Project, V, sources, existingApp));

    SetupResult Result(string id) => Plan().Tasks.Single(task => task.Id == id).Result;

    string Written(string path) {
        Plan().Write();
        return File.ReadAllText(Path.Combine(block, path));
    }

    void Write(string path, string content) {
        var full = Path.Combine(block, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    int RunInit(params string[] args) => RunIn(block, args);

    static int RunIn(string folder, params string[] args) {
        var current = Environment.CurrentDirectory;
        try {
            Environment.CurrentDirectory = folder;
            return CommandList.Run(args);
        } finally {
            Environment.CurrentDirectory = current;
        }
    }
}
