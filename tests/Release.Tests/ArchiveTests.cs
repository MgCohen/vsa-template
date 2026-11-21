using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Tests.Shared;

namespace Release.Tests;

[NonParallelizable]
public sealed class ArchiveTests {
    const string Net8Root = "UMPIRE_NET8_DOTNET_ROOT";

    string folder = null!;
    string tree = null!;
    DotnetResult run = null!;
    JsonNode manifest = null!;

    [OneTimeSetUp]
    public void MakeTheArchive() {
        folder = TempFolder.Create();
        run = Dotnet.Run(Repo.Root, null, "run", Repo.Path("tools", "archive", "archive.cs"), "--",
            "--feed", LocalFeed.Path, "--version", LocalFeed.Version, "--out", Path.Combine(folder, "out"));
        tree = Path.Combine(folder, "tree");
        ZipFile.ExtractToDirectory(Path.Combine(folder, "out", $"umpire-{LocalFeed.Version}.zip"), tree);
        manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(tree, "manifest.json")))!;
    }

    [OneTimeTearDown]
    public void Delete() => TempFolder.Delete(folder);

    [Test]
    public void The_script_makes_the_archive() => Assert.That(run.Succeeded, Is.True, run.Output);

    [TestCase("manifest.json")]
    [TestCase("runtime/netstandard2.1/Umpire.dll")]
    [TestCase("analyzers/dotnet/cs/Umpire.Kernels.Analyzers.dll")]
    [TestCase("analyzers/dotnet/cs/Umpire.Vsa.Analyzers.dll")]
    [TestCase("engine/net8.0/umpire-engine.dll")]
    [TestCase("engine/net8.0/umpire-engine.runtimeconfig.json")]
    public void The_archive_has_the_file_at_its_fixed_path(string path) =>
        Assert.That(File.Exists(Path.Combine(tree, path)), Is.True, path);

    [Test]
    public void The_archive_has_only_the_manifest_and_the_three_parts() {
        var top = Directory.EnumerateFileSystemEntries(tree).Select(Path.GetFileName);
        Assert.That(top, Is.EquivalentTo(new[] { "manifest.json", "runtime", "analyzers", "engine" }));
    }

    [Test]
    public void The_runtime_has_the_third_party_closure() {
        var runtime = Directory.GetFiles(Path.Combine(tree, "runtime", "netstandard2.1")).Select(Path.GetFileName);
        Assert.That(runtime, Does.Contain("Tomlyn.dll").And.Contain("Microsoft.Extensions.DependencyInjection.dll")
            .And.Contain("System.Text.Json.dll"));
    }

    [Test]
    public void The_manifest_gives_the_package_version() =>
        Assert.That(manifest["version"]!.GetValue<string>(), Is.EqualTo(LocalFeed.Version));

    [Test]
    public void The_manifest_gives_the_Roslyn_version_of_the_analyzers() =>
        Assert.That(manifest["analyzers"]!["roslyn"]!.GetValue<string>(), Is.EqualTo("4.10"));

    [Test]
    public void The_manifest_gives_the_runtime_of_the_engine() {
        var engine = manifest["engine"]!;
        var frameworks = engine["frameworks"]!.AsArray().Select(framework => framework!["name"]!.GetValue<string>());

        Assert.That(engine["targetFramework"]!.GetValue<string>(), Is.EqualTo("net8.0"));
        Assert.That(engine["rollForward"]!.GetValue<string>(), Is.EqualTo("Major"));
        Assert.That(frameworks, Is.EqualTo(new[] { "Microsoft.NETCore.App" }));
    }

    [Test]
    public void The_manifest_lists_each_file_of_the_three_parts() {
        var listed = Files().Select(file => file["path"]!.GetValue<string>());
        var inTree = Directory.GetFiles(tree, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(tree, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => path != "manifest.json");

        Assert.That(listed, Is.EquivalentTo(inTree));
    }

    [Test]
    public void Each_file_of_the_manifest_has_its_sha256_and_its_package() {
        Assert.Multiple(() => {
            foreach (var file in Files()) {
                var path = file["path"]!.GetValue<string>();
                using var stream = File.OpenRead(Path.Combine(tree, path));
                var hash = Convert.ToHexStringLower(SHA256.HashData(stream));

                Assert.That(file["sha256"]!.GetValue<string>(), Is.EqualTo(hash), path);
                Assert.That(file["package"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty, path);
                Assert.That(file["packageVersion"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty, path);
            }
        });
    }

    [Test]
    public void The_Umpire_files_have_the_package_version() {
        var umpire = Files().Where(file => file["package"]!.GetValue<string>().StartsWith("Umpire", StringComparison.Ordinal));
        Assert.That(umpire.Select(file => file["packageVersion"]!.GetValue<string>()), Is.All.EqualTo(LocalFeed.Version));
    }

    [Test]
    public void The_engine_of_the_archive_starts_on_a_dotnet_with_only_the_net8_runtime() {
        var root = Environment.GetEnvironmentVariable(Net8Root) is { Length: > 0 } value
            ? value
            : throw new IgnoreException($"Set {Net8Root} to a dotnet folder with only the .NET 8 runtime.");
        var start = new ProcessStartInfo(Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet")) {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("exec");
        start.ArgumentList.Add(Path.Combine(tree, "engine", "net8.0", "umpire-engine.dll"));
        start.Environment["DOTNET_ROOT"] = root;
        start.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd() + error.Result;
        process.WaitForExit();

        Assert.That(output, Does.Contain("needs --Project"), output);
    }

    IEnumerable<JsonNode> Files() => manifest["files"]!.AsArray().Select(file => file!);
}
