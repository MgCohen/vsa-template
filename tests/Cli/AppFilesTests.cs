using Tests.Shared;
using VSA.Cli;
using VSA.Cli.Commands;

namespace Cli.Tests;

// The commands that read the files of an app: doctor and the module lookup of add request. Each test writes a small app.
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

        var found = DoctorCommand.Versions(root);

        Assert.That(found, Does.Contain(("App/Host/App.Host.csproj", "VSA.Host", (string?)V)));
        Assert.That(found.Select(f => f.Version).Distinct(), Is.EqualTo(new[] { V }));
    }

    [Test]
    public void DoctorReadsTheToolManifestInDotConfig() {
        File.Delete(Path.Combine(root, "dotnet-tools.json"));
        Write(".config/dotnet-tools.json", """{ "version": 1, "tools": { "vsa.cli": { "version": "0.0.1", "commands": ["vsa"] } } }""");

        Assert.That(DoctorCommand.Versions(root), Does.Contain((".config/dotnet-tools.json", "vsa.cli", (string?)"0.0.1")));
    }

    [Test]
    public void DoctorReadsTheSdkPinsInProjectFiles() {
        Write("Modules/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/0.2.0" />""");
        Write("Modules/B/B.csproj", """<Project><Sdk Name="VSA.Sdk" Version="0.3.0" /></Project>""");
        Write("Modules/C/C.csproj", """<Project Sdk="VSA.Sdk" />""");

        var pins = DoctorCommand.Versions(root).Where(f => f.Where.StartsWith("Modules/", StringComparison.Ordinal)).ToList();

        Assert.That(pins, Is.EqualTo(new[] { ("Modules/A/A.csproj", "VSA.Sdk", (string?)"0.2.0"), ("Modules/B/B.csproj", "VSA.Sdk", "0.3.0") }));
    }

    [Test]
    public void AddRequestUsesTheModuleNameFromTheDisk() {
        Write("Modules/Billing/Billing.csproj", """<Project Sdk="VSA.Sdk" />""");

        Assert.That(AddRequestCommand.Module(root, "billing"), Is.EqualTo("Billing"));
        Assert.That(() => AddRequestCommand.Module(root, "Shipping"), Throws.TypeOf<VsaException>().With.Message.Contain("does not exist"));
    }

    void Write(string path, string content) {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
