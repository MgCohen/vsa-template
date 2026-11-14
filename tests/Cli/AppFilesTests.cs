using Tests.Shared;
using VSA.Cli;
using VSA.Cli.Commands;

namespace Cli.Tests;

// The commands that read the files of an app: doctor, the module lookup of add request, and the projects of add command. Each test writes a small app.
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
        Write("Features/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/0.2.0" />""");
        Write("Features/B/B.csproj", """<Project><Sdk Name="VSA.Sdk" Version="0.3.0" /></Project>""");
        Write("Features/C/C.csproj", """<Project Sdk="VSA.Sdk" />""");

        var pins = DoctorCommand.Versions(root).Where(f => f.Where.StartsWith("Features/", StringComparison.Ordinal)).ToList();

        Assert.That(pins, Is.EqualTo(new[] { ("Features/A/A.csproj", "VSA.Sdk", (string?)"0.2.0"), ("Features/B/B.csproj", "VSA.Sdk", "0.3.0") }));
    }

    [Test]
    public void UpdateWritesTheVersionToEachPlaceThatDoctorReads() {
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="VSA.Host" Version="0.0.1" /><PackageVersion Include="NUnit" Version="4.0.0" /></ItemGroup></Project>""");
        Write("App/Host/App.Host.csproj", """<Project Sdk="Microsoft.NET.Sdk.Web"><ItemGroup><PackageReference Include="VSA.Host" /><PackageReference Include="VSA.Views" VersionOverride="0.0.2" /></ItemGroup></Project>""");
        Write("Features/A/A.csproj", """<Project Sdk="Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/0.2.0" />""");
        Write("Features/B/B.csproj", """<Project><Sdk Name="VSA.Sdk" Version="0.3.0" /></Project>""");
        Write("Features/C/C.csproj", """<Project Sdk="VSA.Sdk" />""");
        Write("global.json", """{ "msbuild-sdks": { "VSA.Sdk": "0.0.1" } }""");

        var changed = UpdateCommand.SetVersion(root, V);

        Assert.That(changed, Is.EqualTo(new[] { "global.json", "App/Host/App.Host.csproj", "Directory.Packages.props", "Features/A/A.csproj", "Features/B/B.csproj" }));
        Assert.That(DoctorCommand.Versions(root).Select(f => f.Version).Distinct(), Is.EqualTo(new[] { V }));
        Assert.That(File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), Does.Contain("""<PackageVersion Include="NUnit" Version="4.0.0" />"""));
        Assert.That(File.ReadAllText(Path.Combine(root, "Features/A/A.csproj")), Does.Contain($"Microsoft.Build.NoTargets/3.7.0;VSA.Sdk/{V}"));
        Assert.That(UpdateCommand.SetVersion(root, V), Is.Empty);
    }

    [Test]
    public void AddRequestUsesTheModuleNameFromTheDisk() {
        Write("Features/Billing/BillingModule.cs", "namespace Billing;");

        Assert.That(AddRequestCommand.Module(root, "billing"), Is.EqualTo("Billing"));
        Assert.That(() => AddRequestCommand.Module(root, "Shipping"), Throws.TypeOf<VsaException>().With.Message.Contain("does not exist"));
    }

    void Write(string path, string content) {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
