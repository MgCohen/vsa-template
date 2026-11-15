using Tests.Shared;
using VSA.Architecture.Versions;
using VSA.Cli;
using VSA.Cli.Commands;
using VSA.Cli.Scaffolding;

namespace Cli.Tests;

public sealed class AppFilesTests {
    static readonly string V = ToolVersion.Current;
    string root = "";

    [SetUp]
    public void SetUp() {
        root = TempFolder.Create();
        Write("global.json", $$"""{ "msbuild-sdks": { "VSA.Sdk": "{{V}}" } }""");
        Write("dotnet-tools.json", $$"""{ "version": 1, "tools": { "vsa.cli": { "version": "{{V}}", "commands": ["vsa"] } } }""");
    }

    [TearDown]
    public void TearDown() => TempFolder.Delete(root);

    [Test]
    public void DoctorUsesTheCentralVersionOfAPackageReference() {
        Write("Directory.Packages.props", $"""<Project><ItemGroup><PackageVersion Include="VSA.Host" Version="{V}" /></ItemGroup></Project>""");
        Write("App/Host/App.Host.csproj", """<Project Sdk="Microsoft.NET.Sdk.Web"><ItemGroup><PackageReference Include="VSA.Host" /></ItemGroup></Project>""");

        Assert.That(Slots(), Does.Contain($"App/Host/App.Host.csproj VSA.Host {V}"));
        Assert.That(DoctorCommand.Slots(root).Select(slot => slot.Version).Distinct(), Is.EqualTo(new[] { V }));
    }

    [Test]
    public void DoctorReadsTheToolManifestInDotConfig() {
        File.Delete(Path.Combine(root, "dotnet-tools.json"));
        Write(".config/dotnet-tools.json", """{ "version": 1, "tools": { "vsa.cli": { "version": "0.0.1", "commands": ["vsa"] } } }""");

        Assert.That(Slots(), Does.Contain(".config/dotnet-tools.json vsa.cli 0.0.1"));
    }

    [Test]
    public void AnAppWithNoToolManifestHasNoToolManifestSlot() {
        File.Delete(Path.Combine(root, "dotnet-tools.json"));

        Assert.That(Slots(), Is.EqualTo(new[] { $"this tool VSA.Cli {V}", $"global.json VSA.Sdk {V}" }));
    }

    [Test]
    public void DoctorReadsTheSdkPinsInProjectFiles() {
        Write("Features/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/0.2.0" />""");
        Write("Features/B/B.csproj", """<Project><Sdk Name="VSA.Sdk" Version="0.3.0" /></Project>""");
        Write("Features/C/C.csproj", """<Project Sdk="VSA.Sdk" />""");

        var pins = Slots().Where(slot => slot.StartsWith("Features/", StringComparison.Ordinal));

        Assert.That(pins, Is.EqualTo(new[] { "Features/A/A.csproj VSA.Sdk 0.2.0", "Features/B/B.csproj VSA.Sdk 0.3.0" }));
    }

    [Test]
    public void ThePackageOfTheBuildCheckIsASlot() {
        Write("App.csproj", """<Project Sdk="VSA.Sdk"><ItemGroup><PackageReference Include="VSA.Architecture.Build" Version="0.0.1" PrivateAssets="all" /></ItemGroup></Project>""");

        var mismatches = VersionCheck.Mismatches(DoctorCommand.Slots(root), V);

        Assert.That(mismatches.Select(slot => $"{slot.File} {slot.Package} {slot.Version} {slot.Line}"), Is.EqualTo(new[] { "App.csproj VSA.Architecture.Build 0.0.1 1" }));
        Assert.That(VersionCheck.Message(mismatches, V), Does.Contain("App.csproj: VSA.Architecture.Build 0.0.1"));
    }

    [Test]
    public void APackageWithNoVersionAndNoCentralVersionHasNoSlot() {
        Write("App.csproj", """<Project Sdk="VSA.Sdk"><ItemGroup><PackageReference Include="VSA.Host" /></ItemGroup></Project>""");

        Assert.That(Slots(), Has.None.Contains("VSA.Host"));
    }

    [Test]
    public void UpdateWritesTheVersionToEachPlaceThatDoctorReads() {
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="VSA.Host" Version="0.0.1" /><PackageVersion Include="NUnit" Version="4.0.0" /></ItemGroup></Project>""");
        Write("App/Host/App.Host.csproj", """<Project Sdk="Microsoft.NET.Sdk.Web"><ItemGroup><PackageReference Include="VSA.Host" /><PackageReference Include="VSA.Views" VersionOverride="0.0.2" /><PackageReference Include="VSA.Architecture.Build" Version="0.0.3" PrivateAssets="all" /></ItemGroup></Project>""");
        Write("Features/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/0.2.0" />""");
        Write("Features/B/B.csproj", """<Project><Sdk Name="VSA.Sdk" Version="0.3.0" /></Project>""");
        Write("Features/C/C.csproj", """<Project Sdk="VSA.Sdk" />""");
        Write("global.json", """{ "msbuild-sdks": { "VSA.Sdk": "0.0.1" } }""");

        var changed = UpdateCommand.SetVersion(root, V);

        Assert.That(changed, Is.EqualTo(new[] { "global.json", "App/Host/App.Host.csproj", "Directory.Packages.props", "Features/A/A.csproj", "Features/B/B.csproj" }));
        Assert.That(DoctorCommand.Slots(root).Select(slot => slot.Version).Distinct(), Is.EqualTo(new[] { V }));
        Assert.That(File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), Does.Contain("""<PackageVersion Include="NUnit" Version="4.0.0" />"""));
        Assert.That(File.ReadAllText(Path.Combine(root, "Features/A/A.csproj")), Does.Contain($"Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/{V}"));
        Assert.That(UpdateCommand.SetVersion(root, V), Is.Empty);
    }

    [Test]
    public void AddRequestUsesTheModuleNameFromTheDisk() {
        Write("Features/Billing/BillingModule.cs", "namespace Billing;");

        Assert.That(ModuleFolder.Find(root, "billing"), Is.EqualTo("Billing"));
        Assert.That(() => ModuleFolder.Find(root, "Shipping"), Throws.TypeOf<VsaException>().With.Message.Contain("does not exist"));
    }

    string[] Slots() => [.. DoctorCommand.Slots(root).Select(slot => $"{slot.File} {slot.Package} {slot.Version}")];

    void Write(string path, string content) {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
