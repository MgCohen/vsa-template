using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Tests.Shared;

namespace Release.Tests;

[NonParallelizable]
public sealed class ArchiveTests {
    const string Net8Root = "UMPIRE_NET8_DOTNET_ROOT";
    const string Notices = "THIRD-PARTY-NOTICES.txt";

    string folder = null!;
    string tree = null!;
    JsonNode manifest = null!;

    [OneTimeSetUp]
    public void MakeTheArchive() {
        folder = TempFolder.Create();
        var run = Dotnet.Run(Repo.Root, null, "run", Repo.Path("tools", "archive", "archive.cs"), "--",
            "--feed", LocalFeed.Path, "--version", LocalFeed.Version, "--out", Path.Combine(folder, "out"));
        Assert.That(run.Succeeded, Is.True,
            "The archive script failed. It needs nuget.org for the third-party packages."
            + Environment.NewLine + run.Output);
        tree = Path.Combine(folder, "tree");
        ZipFile.ExtractToDirectory(Path.Combine(folder, "out", $"umpire-{LocalFeed.Version}.zip"), tree);
        manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(tree, "manifest.json")))!;
    }

    [OneTimeTearDown]
    public void Delete() => TempFolder.Delete(folder);

    [TestCase("manifest.json")]
    [TestCase(Notices)]
    [TestCase("runtime/netstandard2.0/Umpire.dll")]
    [TestCase("analyzers/dotnet/cs/Umpire.Kernels.Analyzers.dll")]
    [TestCase("analyzers/dotnet/cs/Umpire.Vsa.Analyzers.dll")]
    [TestCase("engine/net8.0/umpire-engine.dll")]
    [TestCase("engine/net8.0/umpire-engine.runtimeconfig.json")]
    public void The_archive_has_the_file_at_its_fixed_path(string path) =>
        Assert.That(File.Exists(Path.Combine(tree, path)), Is.True, path);

    [Test]
    public void The_archive_has_only_the_manifest_the_notices_and_the_three_parts() {
        var top = Directory.EnumerateFileSystemEntries(tree).Select(Path.GetFileName);
        Assert.That(top, Is.EquivalentTo(new[] { "manifest.json", Notices, "runtime", "analyzers", "engine" }));
    }

    [Test]
    public void The_runtime_has_the_third_party_closure() {
        var runtime = Runtime().Select(FileName);
        Assert.That(runtime, Does.Contain("Microsoft.Extensions.DependencyInjection.dll")
            .And.Not.Contain("Tomlyn.dll").And.Not.Contain("System.IO.Pipelines.dll"));
    }

    [Test]
    public void Each_Microsoft_Extensions_file_of_the_runtime_is_version_8() {
        var extensions = Runtime()
            .Where(file => FileName(file).StartsWith("Microsoft.Extensions.", StringComparison.Ordinal))
            .ToList();

        Assert.That(extensions, Is.Not.Empty);
        Assert.Multiple(() => {
            foreach (var file in extensions) {
                Assert.That(Text(file, "assemblyVersion"), Does.StartWith("8.0."), FileName(file));
                Assert.That(Text(file, "packageVersion"), Does.StartWith("8.0."), FileName(file));
            }
        });
    }

    [Test]
    public void Each_file_of_the_runtime_comes_from_a_netstandard_asset() =>
        Assert.That(Runtime().Select(file => Text(file, "asset")), Is.All.StartsWith("lib/netstandard2."));

    [Test]
    public void The_analyzer_files_come_from_the_analyzer_package() {
        var analyzers = Files().Where(file => PathOf(file).StartsWith("analyzers/", StringComparison.Ordinal));
        Assert.That(analyzers.Select(file => Text(file, "package")), Is.All.EqualTo("Umpire.Vsa.Analyzers"));
    }

    [Test]
    public void A_file_that_is_not_a_dll_has_no_assembly_version() {
        var config = Files().Single(file => PathOf(file) == "engine/net8.0/umpire-engine.runtimeconfig.json");
        Assert.That(config["assemblyVersion"], Is.Null);
    }

    [Test]
    public void The_notices_give_the_license_of_each_third_party_package() {
        var notices = File.ReadAllText(Path.Combine(tree, Notices));
        Assert.That(notices, Does.Not.Contain("Tomlyn").And.Contain("TngTech.ArchUnitNET")
            .And.Contain("Umpire.Kernels.Analyzers.dll embeds"));
    }

    [Test]
    public void The_manifest_gives_the_commit() =>
        Assert.That(manifest["commit"]!.GetValue<string>(), Does.Match("^[0-9a-f]{40}$"));

    [Test]
    public void The_manifest_gives_the_package_version() =>
        Assert.That(manifest["version"]!.GetValue<string>(), Is.EqualTo(LocalFeed.Version));

    [Test]
    public void The_manifest_gives_the_Roslyn_version_of_the_analyzers() =>
        Assert.That(manifest["analyzers"]!["roslyn"]!.GetValue<string>(), Is.EqualTo("4.10"));

    [Test]
    public void The_manifest_gives_the_runtime_of_the_engine() {
        var engine = manifest["engine"]!;
        var frameworks = engine["frameworks"]!.AsArray()
            .Select(framework => $"{Text(framework!, "name")} {Text(framework!, "version")}");

        Assert.That(Text(engine, "targetFramework"), Is.EqualTo("net8.0"));
        Assert.That(Text(engine, "rollForward"), Is.EqualTo("Major"));
        Assert.That(frameworks, Is.EqualTo(new[] { "Microsoft.NETCore.App 8.0.0" }));
    }

    [Test]
    public void The_manifest_gives_the_Umpire_build_of_the_engine() =>
        Assert.That(Text(manifest["engine"]!, "umpireBuild"), Is.EqualTo("netstandard2.0"));

    [Test]
    public void The_manifest_lists_each_file_of_the_three_parts() {
        var listed = Files().Select(PathOf);
        var inTree = Directory.GetFiles(tree, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(tree, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => path is not "manifest.json" and not Notices);

        Assert.That(listed, Is.EquivalentTo(inTree));
    }

    [Test]
    public void Each_file_of_the_manifest_has_its_sha256_and_its_package() {
        Assert.Multiple(() => {
            foreach (var file in Files()) {
                var path = PathOf(file);
                using var stream = File.OpenRead(Path.Combine(tree, path));
                var hash = Convert.ToHexStringLower(SHA256.HashData(stream));

                Assert.That(Text(file, "sha256"), Is.EqualTo(hash), path);
                Assert.That(file["package"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty, path);
                Assert.That(file["packageVersion"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty, path);
                Assert.That(file["asset"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty, path);
            }
        });
    }

    [Test]
    public void The_Umpire_files_have_the_package_version() {
        var umpire = Files().Where(file => Text(file, "package").StartsWith("Umpire", StringComparison.Ordinal));
        Assert.That(umpire.Select(file => Text(file, "packageVersion")), Is.All.EqualTo(LocalFeed.Version));
    }

    [Test]
    public void The_engine_of_the_archive_starts_on_a_dotnet_with_only_the_net8_runtime() {
        var output = Engine(Net8DotnetRoot());

        Assert.That(output, Does.Contain("needs --Project"), output);
    }

    [Test]
    public void The_engine_of_the_archive_checks_an_app_with_its_own_concept_compiled_against_the_runtime() {
        var root = Net8DotnetRoot();
        var app = AppOnTheRuntime(withStray: false);
        var output = Engine(root, "--project", app, "--block", app, "--bin", Path.Combine(app, "out"),
            "--stage", "Check", "--projectchecks", "false", "--mode", "Error");

        Assert.That(output, Does.Contain("umpire-engine: 0 errors,").And.Not.Contain("UMP030001"), output);
    }

    [Test]
    public void The_engine_of_the_archive_finds_a_wrong_type_in_an_app_compiled_against_the_runtime() {
        var root = Net8DotnetRoot();
        var app = AppOnTheRuntime(withStray: true);
        var output = Engine(root, "--project", app, "--block", app, "--bin", Path.Combine(app, "out"),
            "--stage", "Check", "--projectchecks", "false", "--mode", "Error");

        Assert.That(output, Does.Contain("error UMP020202").And.Contain("Stray"), output);
    }

    static string PathOf(JsonNode file) => Text(file, "path");

    static string FileName(JsonNode file) => Path.GetFileName(PathOf(file));

    static string Text(JsonNode node, string key) => node[key]!.GetValue<string>();

    static string Net8DotnetRoot() =>
        Environment.GetEnvironmentVariable(Net8Root) is { Length: > 0 } value
            ? value
            : throw new IgnoreException($"Set {Net8Root} to a dotnet folder with only the .NET 8 runtime.");

    IEnumerable<JsonNode> Files() => manifest["files"]!.AsArray().Select(file => file!);

    IEnumerable<JsonNode> Runtime() =>
        Files().Where(file => PathOf(file).StartsWith("runtime/", StringComparison.Ordinal));

    string AppOnTheRuntime(bool withStray) {
        var app = Path.Combine(folder, withStray ? "stray-app" : "concept-app");
        var umpire = Path.Combine(tree, "runtime", "netstandard2.0", "Umpire.dll");
        Directory.CreateDirectory(Path.Combine(app, "App", "Host"));
        File.WriteAllText(Path.Combine(app, "DiceGame.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.1</TargetFramework>
                <LangVersion>latest</LangVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>disable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <Reference Include="Umpire" HintPath="{umpire}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(app, "App", "Host", "DiceShell.cs"), """
            namespace DiceGame.App.Host;

            using Umpire.Kernels;
            using Umpire.Vsa.Composition;

            public sealed class DiceShell : IAppEntry<VsaKernel> { }
            """);
        Directory.CreateDirectory(Path.Combine(app, "Definition"));
        File.WriteAllText(Path.Combine(app, "Definition", "DiceArchitecture.cs"), """
            namespace DiceGame.Definition;

            using Umpire.Architecture;
            using Umpire.Vsa.Definition;

            public sealed class DiceArchitecture : VsaArchitecture {
                protected override void Rules(RuleBook rules) => rules.For<Views>().Uses<HostViews>();
            }

            public sealed record HostViews : External {
                public override string Assembly => "Host.Views";
            }
            """);
        if (withStray) {
            Directory.CreateDirectory(Path.Combine(app, "Misc"));
            File.WriteAllText(Path.Combine(app, "Misc", "Stray.cs"),
                "namespace DiceGame.Misc;\npublic sealed class Stray { }\n");
        }
        var build = Dotnet.Run(app, null, "build", "-o", Path.Combine(app, "out"));
        Assert.That(build.Succeeded, Is.True, build.Output);
        return app;
    }

    string Engine(string root, params string[] arguments) {
        var dotnet = Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var start = new ProcessStartInfo(dotnet) {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("exec");
        start.ArgumentList.Add(Path.Combine(tree, "engine", "net8.0", "umpire-engine.dll"));
        foreach (var argument in arguments) {
            start.ArgumentList.Add(argument);
        }
        start.Environment["DOTNET_ROOT"] = root;
        start.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd() + error.Result;
        process.WaitForExit();
        return output;
    }
}
