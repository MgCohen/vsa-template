using Tests.Shared;
using Umpire.Cli;
using Umpire.Cli.Commands;
using Umpire.Cli.Scaffolding;
using Umpire.Cli.Scaffolding.Setup;

namespace Cli.Tests;

public sealed class SetupTests {
    const string Project = "Shop.csproj";
    const string WebProject = "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n";
    static readonly string V = ToolVersion.Current;
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

        Assert.That(plan.Tasks.Where(task => task.Id != "umpire-config").Select(task => task.Result),
            Has.All.TypeOf<SetupResult.Change>());
        Assert.That(plan.Files.Select(file => file.Path), Is.EquivalentTo(new[] { "global.json", "nuget.config", ".gitignore", Project }));
        foreach (var path in new[] { "global.json", "nuget.config", ".gitignore" }) {
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
    public void APlainWebProjectGetsTheUmpireWebSdkWithTheVersion() =>
        Assert.That(Written(Project), Is.EqualTo(WebProject.Replace("Microsoft.NET.Sdk.Web", $"Umpire.Sdk.Web/{V}")));

    [Test]
    public void AnUmpireProjectWithNoVersionGetsTheVersion() {
        Write(Project, "<Project Sdk=\"Umpire.Sdk\" />\n");

        Assert.That(Written(Project), Is.EqualTo($"<Project Sdk=\"Umpire.Sdk/{V}\" />\n"));
    }

    [Test]
    public void AnUmpireProjectWithTheVersionIsDone() {
        Write(Project, $"<Project Sdk=\"Umpire.Sdk.Web/{V}\" />\n");

        Assert.That(Result("umpire-sdk"), Is.TypeOf<SetupResult.Done>());
    }

    [Test]
    public void AProjectPinOfAnotherVersionIsByHand() {
        Write(Project, "<Project Sdk=\"Umpire.Sdk/0.0.1\" />\n");

        Assert.That(Result("umpire-sdk"), Is.TypeOf<SetupResult.ByHand>().With.Property("Message").Contain("umpire update"));
    }

    [Test]
    public void AnUmpireSdkElementIsByHand() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <Sdk Name=\"Umpire.Sdk\" />\n</Project>\n");

        Assert.That(Result("umpire-sdk"), Is.TypeOf<SetupResult.ByHand>().With.Property("Message").Contain("main SDK"));
    }

    [Test]
    public void AnotherMainSdkIsByHand() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk.Razor\" />\n");

        Assert.That(Result("umpire-sdk"), Is.TypeOf<SetupResult.ByHand>().With.Property("Message").Contain("Microsoft.NET.Sdk.Razor"));
    }

    [Test]
    public void ABlockWithASecondProjectGetsAnUmpireconfigFile() {
        Write("Tests/Shop.Tests.csproj", "<Project Sdk=\"Umpire.Sdk\" />\n");

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
    public void ANewGlobalJsonKeepsTheSdksOfTheParentWithNoUmpirePinAndAddsNoTestRunner() {
        File.WriteAllText(Path.Combine(parent, "global.json"),
            "{ \"sdk\": { \"version\": \"10.0.200\" }, \"msbuild-sdks\": { \"Other.Sdk\": \"1.2.3\", \"Umpire.Sdk\": \"0.0.1\" } }");

        var json = Written("global.json");

        Assert.That(json, Does.Contain("\"version\": \"10.0.200\""));
        Assert.That(json, Does.Contain("\"Other.Sdk\": \"1.2.3\""));
        Assert.That(json, Does.Not.Contain("Umpire.Sdk"));
        Assert.That(json, Does.Not.Contain("test"));
    }

    [Test]
    public void ANewGlobalJsonKeepsTheTestRunnerOfTheParent() {
        File.WriteAllText(Path.Combine(parent, "global.json"), "{ \"test\": { \"runner\": \"VSTest\" } }");

        Assert.That(Written("global.json"), Does.Contain("\"runner\": \"VSTest\""));
    }

    [Test]
    public void AProjectWithTabsKeepsItsTabs() {
        Write(Project, "<Project Sdk=\"Microsoft.NET.Sdk\">\n\t<PropertyGroup>\n\t\t<OutputType>Exe</OutputType>\n\t</PropertyGroup>\n</Project>\n");

        Assert.That(Written(Project), Is.EqualTo(
            $"<Project Sdk=\"Umpire.Sdk/{V}\">\n\t<PropertyGroup>\n\t\t<OutputType>Exe</OutputType>\n\t</PropertyGroup>\n</Project>\n"));
    }

    [Test]
    public void ANugetConfigWithFourSpacesGetsFourSpaces() {
        Write("nuget.config", "<configuration>\n    <packageSources>\n        <add key=\"company\" value=\"https://example.com/index.json\" />\n    </packageSources>\n</configuration>\n");

        var config = Written("nuget.config");

        Assert.That(config, Does.Contain("\n        <add key=\"umpire\" value=\"https://nuget.pkg.github.com/MgCohen/index.json\" />\n    </packageSources>"));
        Assert.That(config, Does.Contain("\n    <packageSourceCredentials>\n        <umpire>\n            <add key=\"Username\" value=\"umpire\" />"));
    }

    [Test]
    public void AFileWithMixedLineEndingsKeepsTheEndingOfEachLine() {
        Write(".gitignore", "bin/\r\nobj/\r\nnode_modules/\n");

        Plan().Write();

        Assert.That(File.ReadAllText(Path.Combine(block, ".gitignore")),
            Is.EqualTo("bin/\r\nobj/\r\nnode_modules/\nartifacts~/\r\nTestResults/\r\n*.user\r\n.vs/\r\n"));
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
    public void NewAppDoesNotReadTheParentFiles() {
        File.WriteAllText(Path.Combine(parent, "global.json"), "{ \"sdk\": { \"version\": \"10.0.200\" } }");

        Assert.That(Plan(keepsParentFiles: false).Files.Single(file => file.Path == "global.json").Content,
            Is.EqualTo(Templates.SetupFile("global.json", V)));
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
        Assert.That(File.Exists(Path.Combine(block, "global.json")), Is.False);
    }

    [Test]
    public void InitRejectsACheckValueThatIsNotABoolean() =>
        Assert.That(() => RunInit("init", "--check", "yes"), Throws.TypeOf<UmpireException>().With.Message.Contain("--check true"));

    SetupPlan Plan(bool keepsParentFiles = true, params string[] sources) =>
        SetupTasks.Plan(new SetupContext(block, Project, V, sources, keepsParentFiles));

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
