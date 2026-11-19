using System.Text;
using Tests.Shared;
using Umpire.Engine.Versions;
using Umpire.Cli;
using Umpire.Cli.Commands;
using Umpire.Cli.Scaffolding;

namespace Cli.Tests;

public sealed class AppFilesTests {
    static readonly string V = ToolVersion.Current;
    string root = "";

    [SetUp]
    public void SetUp() => root = TempFolder.Create();

    [TearDown]
    public void TearDown() => TempFolder.Delete(root);

    [Test]
    public void DoctorUsesTheCentralVersionOfAPackageReference() {
        Write("Directory.Packages.props", $"""<Project><ItemGroup><PackageVersion Include="Umpire.Vsa.Hosts.Web" Version="{V}" /></ItemGroup></Project>""");
        Write("App/Host/App.Host.csproj", """<Project Sdk="Microsoft.NET.Sdk.Web"><ItemGroup><PackageReference Include="Umpire.Vsa.Hosts.Web" /></ItemGroup></Project>""");

        Assert.That(Slots(), Does.Contain($"App/Host/App.Host.csproj Umpire.Vsa.Hosts.Web {V}"));
        Assert.That(DoctorCommand.Slots(root).Select(slot => slot.Version).Distinct(), Is.EqualTo(new[] { V }));
    }

    [Test]
    public void TestFindsEachProjectWhoseNameEndsWithTests() {
        Write("App.csproj", "<Project />");
        Write("Tests/App.Tests.csproj", "<Project />");
        Write("Packs/Ecs/Tests/Ecs.Tests.csproj", "<Project />");
        Write("Tests/Helpers.csproj", "<Project />");
        Write("artifacts~/obj/Old.Tests.csproj", "<Project />");
        Write("Tests/bin/Copy.Tests.csproj", "<Project />");

        Assert.That(TestCommand.TestProjects(root),
            Is.EqualTo(new[] { "Packs/Ecs/Tests/Ecs.Tests.csproj", "Tests/App.Tests.csproj" }));
    }

    [Test]
    public void DoctorReadsTheSdkPinsInProjectFiles() {
        Write("Features/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;Umpire.Sdk/0.2.0" />""");
        Write("Features/B/B.csproj", """<Project><Sdk Name="Umpire.Sdk" Version="0.3.0" /></Project>""");
        Write("Features/C/C.csproj", """<Project Sdk="Umpire.Sdk" />""");

        var pins = Slots().Where(slot => slot.StartsWith("Features/", StringComparison.Ordinal));

        Assert.That(pins, Is.EqualTo(new[] { "Features/A/A.csproj Umpire.Sdk 0.2.0", "Features/B/B.csproj Umpire.Sdk 0.3.0" }));
    }

    [Test]
    public void ThePackageOfTheBuildCheckIsASlot() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk"><ItemGroup><PackageReference Include="Umpire.Hosts.Web" Version="0.0.1" PrivateAssets="all" /></ItemGroup></Project>""");

        var mismatches = VersionCheck.Mismatches(DoctorCommand.Slots(root), V);

        Assert.That(mismatches.Select(slot => $"{slot.File} {slot.Package} {slot.Version} {slot.Line}"), Is.EqualTo(new[] { "App.csproj Umpire.Hosts.Web 0.0.1 1" }));
        Assert.That(VersionCheck.Message(mismatches, V), Does.Contain("App.csproj: Umpire.Hosts.Web 0.0.1"));
    }

    [Test]
    public void APackageWithNoVersionAndNoCentralVersionFailsTheCheck() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk"><ItemGroup><PackageReference Include="Umpire.Vsa.Hosts.Web" /><PackageReference Update="Umpire.Vsa.Analyzers" PrivateAssets="all" /></ItemGroup></Project>""");

        var mismatches = VersionCheck.Mismatches(DoctorCommand.Slots(root), V);

        Assert.That(mismatches.Select(slot => slot.Package), Is.EqualTo(new[] { "Umpire.Vsa.Hosts.Web" }));
        Assert.That(VersionCheck.Message(mismatches, V), Does.Contain("App.csproj: Umpire.Vsa.Hosts.Web has no version; add Version or a central PackageVersion"));
    }

    [Test]
    public void APackageUsesTheCentralVersionAboveTheBlockFolder() {
        var app = Path.Combine(root, "src");
        Write("Directory.Packages.props", $"""<Project><ItemGroup><PackageVersion Include="Umpire.Hosts.Web" Version="{V}" /></ItemGroup></Project>""");
        Write("src/App.csproj", """<Project Sdk="Umpire.Sdk"><ItemGroup><PackageReference Include="Umpire.Hosts.Web" PrivateAssets="all" /></ItemGroup></Project>""");

        var slots = VersionSlots.InApp(app).SelectMany(file => file.Slots).ToList();

        Assert.That(slots.Select(slot => $"{slot.File} {slot.Package} {slot.Version}"), Is.EqualTo(new[] { $"App.csproj Umpire.Hosts.Web {V}" }));
    }

    [Test]
    public void AVersionWithAPropertyIsNotCheckedAndNotWritten() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk"><ItemGroup><PackageReference Include="Umpire.Vsa.Hosts.Web" Version="$(UmpireVersion)" /></ItemGroup></Project>""");

        var slot = DoctorCommand.Slots(root).Single(slot => slot.Package == "Umpire.Vsa.Hosts.Web");

        Assert.That(VersionCheck.Mismatches([slot], V), Is.Empty);
        Assert.That(VersionCheck.Shown(slot), Is.EqualTo("(not checked: $(UmpireVersion))"));
        Assert.That(UpdateCommand.SetVersion(root, V), Is.Empty);
    }

    [Test]
    public void AnExactRangeCountsAsItsVersion() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk"><ItemGroup><PackageReference Include="Umpire.Vsa.Hosts.Web" Version="[0.0.1]" /></ItemGroup></Project>""");

        Assert.That(Slots(), Does.Contain("App.csproj Umpire.Vsa.Hosts.Web 0.0.1"));
        UpdateCommand.SetVersion(root, V);
        Assert.That(File.ReadAllText(Path.Combine(root, "App.csproj")), Does.Contain($"Version=\"[{V}]\""));
    }

    [Test]
    public void DoctorReadsTheWebSdkPinAndNotGlobalJson() {
        Write("global.json", """{ "msbuild-sdks": { "Umpire.Sdk": "0.0.1" } }""");
        Write("App.csproj", """<Project Sdk="Umpire.Sdk.Web/0.0.2" />""");

        Assert.That(Slots(), Is.EqualTo(new[] { $"this tool Umpire.Cli {V}", "App.csproj Umpire.Sdk.Web 0.0.2" }));
    }

    [Test]
    public void DoctorStopsOnAGlobalJsonPinAndAnUmpireSdkWithNoVersion() {
        Write("global.json", """{ "msbuild-sdks": { "Umpire.Sdk": "0.0.1" } }""");
        Write("App.csproj", """<Project Sdk="Umpire.Sdk" />""");
        Write("Lib/Lib.csproj", $"""<Project Sdk="Umpire.Sdk/{V}" />""");

        var unpinned = VersionSlots.UnpinnedSdks(root);

        Assert.That(unpinned, Has.Count.EqualTo(2));
        Assert.That(unpinned[0], Does.Contain("global.json").And.Contain("Remove the entry"));
        Assert.That(unpinned[1], Does.StartWith("App.csproj: an Umpire SDK with no version"));
        Assert.That(() => InFolder(() => CommandList.Run(["doctor"])),
            Throws.TypeOf<UmpireException>().With.Message.Contain("Change these by hand"));
    }

    [Test]
    public void UpdateWithNoVersionWritesTheVersionOfTheTool() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk.Web/0.0.1" />""");

        var exitCode = InFolder(() => CommandList.Run(["update"]));

        Assert.That(exitCode, Is.Zero);
        Assert.That(File.ReadAllText(Path.Combine(root, "App.csproj")), Does.Contain($"Umpire.Sdk.Web/{V}"));
    }

    [Test]
    public void UpdateKeepsTheLfLineEndingsOfAProjectFile() {
        Write("App.csproj",
            "<Project Sdk=\"Umpire.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Umpire.Vsa.Hosts.Web\" Version=\"0.0.1\" />\n  </ItemGroup>\n</Project>\n");

        UpdateCommand.SetVersion(root, V);

        var text = File.ReadAllText(Path.Combine(root, "App.csproj"));
        Assert.That(text, Does.Contain($"Version=\"{V}\"").And.Not.Contain("\r\n"));
    }

    [Test]
    public void TheBuildOutputGivesOnlyTheVsaPackagesOfTheDepsFile() {
        Write("bin/App.deps.json", """
            { "libraries": {
                "Umpire.Vsa.Domain/0.0.1": { "type": "package" },
                "Umpire.Shop/1.0.0": { "type": "project" },
                "NUnit/4.0.0": { "type": "package" } } }
            """);
        Write("bin/Umpire.Shop.dll", "");

        var slots = VersionSlots.InBin(Path.Combine(root, "bin"));

        Assert.That(slots.Select(slot => $"{slot.File} {slot.Package} {slot.Version}"), Is.EqualTo(new[] { "App.deps.json Umpire.Vsa.Domain 0.0.1" }));
    }

    [Test]
    public void UpdateWritesTheVersionToEachPlaceThatDoctorReads() {
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="Umpire.Vsa.Hosts.Web" Version="0.0.1" /><PackageVersion Include="NUnit" Version="4.0.0" /></ItemGroup></Project>""");
        Write("App/Host/App.Host.csproj", """<Project Sdk="Microsoft.NET.Sdk.Web"><ItemGroup><PackageReference Include="Umpire.Vsa.Hosts.Web" /><PackageReference Include="Umpire.Vsa.Views" VersionOverride="0.0.2" /><PackageReference Include="Umpire.Hosts.Web" Version="0.0.3" PrivateAssets="all" /></ItemGroup></Project>""");
        Write("Features/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;Umpire.Sdk/0.0.5" />""");
        Write("Features/B/B.csproj", """<Project><Sdk Name="Umpire.Sdk" Version="0.0.6" /></Project>""");
        Write("Features/C/C.csproj", """<Project Sdk="Umpire.Sdk" />""");
        Write("Features/W/W.csproj", """<Project Sdk="Umpire.Sdk.Web/0.0.4" />""");

        var changed = UpdateCommand.SetVersion(root, V);

        Assert.That(changed, Is.EqualTo(new[] {
            "App/Host/App.Host.csproj", "Directory.Packages.props", "Features/A/A.csproj", "Features/B/B.csproj", "Features/W/W.csproj" }));
        Assert.That(DoctorCommand.Slots(root).Select(slot => slot.Version).Distinct(), Is.EqualTo(new[] { V }));
        Assert.That(File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), Does.Contain("""<PackageVersion Include="NUnit" Version="4.0.0" />"""));
        Assert.That(File.ReadAllText(Path.Combine(root, "Features/A/A.csproj")), Does.Contain($"Microsoft.Build.NoTargets/3.7.0;Umpire.Sdk/{V}"));
        Assert.That(File.ReadAllText(Path.Combine(root, "Features/W/W.csproj")), Does.Contain($"Umpire.Sdk.Web/{V}"));
        Assert.That(UpdateCommand.SetVersion(root, V), Is.Empty);
    }

    [Test]
    public void AddRequestUsesTheModuleNameFromTheDisk() {
        Write("Features/Billing/BillingModule.cs", "namespace Billing;");

        Assert.That(ModuleFolder.Find(root, "billing"), Is.EqualTo("Billing"));
        Assert.That(() => ModuleFolder.Find(root, "Shipping"), Throws.TypeOf<UmpireException>().With.Message.Contain("does not exist"));
    }

    int InFolder(Func<int> run) {
        var current = Environment.CurrentDirectory;
        try {
            Environment.CurrentDirectory = root;
            return run();
        } finally {
            Environment.CurrentDirectory = current;
        }
    }

    string[] Slots() => [.. DoctorCommand.Slots(root).Select(slot => $"{slot.File} {slot.Package} {slot.Version}")];

    void Write(string path, string content) {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
