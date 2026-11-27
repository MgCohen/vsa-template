using System.Diagnostics;
using Tests.Shared;

namespace Framework.Tests.Unity;

[NonParallelizable]
public sealed class AnalyzerLoadTests {
    const string LongLine = "public sealed class Probe { public string Name { get; } = \"a name that is long\"; }\n";
    const string Config = "umpire.UMP010101.max = 20\n";
    const string Net8Root = "UMPIRE_NET8_DOTNET_ROOT";

    string output = "";

    [OneTimeSetUp]
    public void CompileWithTheUnityCompiler() {
        if (Net8Compiler.Find() is not { } compiler) {
            Assert.Ignore($"No .NET 8 SDK compiler. Install a .NET 8 SDK, or set {Net8Root} "
                + "to the dotnet folder of a .NET 8 SDK, for example the one in a Unity 6 editor.");
            return;
        }
        var folder = TempFolder.Create();
        File.WriteAllText(Path.Combine(folder, "Probe.cs"), LongLine);
        File.WriteAllText(Path.Combine(folder, ".umpireconfig"), Config);
        var analyzers = BuiltAssembly.AnalyzerFiles().Select(file => {
            var copy = Path.Combine(folder, file);
            File.Copy(Path.Combine(BuiltAssembly.AnalyzerFolder, file), copy);
            return copy;
        }).ToList();
        output = compiler.Compile(folder, [
            "-noconfig", "-nologo", "-target:library", $"-out:{Path.Combine(folder, "Probe.dll")}",
            .. compiler.References.Select(reference => $"-reference:{reference}"),
            .. analyzers.Select(analyzer => $"-analyzer:{analyzer}"),
            $"-additionalfile:{Path.Combine(folder, ".umpireconfig")}",
            Path.Combine(folder, "Probe.cs"),
        ]);
    }

    [Test]
    public void Each_analyzer_loads() => Assert.That(output, Does.Not.Contain("CS8032"), output);

    [Test]
    public void No_analyzer_throws() => Assert.That(output, Does.Not.Contain("AD0001"), output);

    [Test]
    public void The_config_reader_loads() => Assert.That(output, Does.Not.Contain("UMP010003"), output);

    [Test]
    public void A_rule_runs_with_the_value_of_the_config_file() =>
        Assert.That(output, Does.Contain("UMP010101").And.Contain("the limit is 20"), output);

    sealed record Net8Compiler(string Dotnet, string Csc, IReadOnlyList<string> References) {
        public static Net8Compiler? Find() => Roots().Select(Of).FirstOrDefault(compiler => compiler is not null);

        public string Compile(string folder, IEnumerable<string> arguments) {
            var start = new ProcessStartInfo(Dotnet) {
                WorkingDirectory = folder,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add(Csc);
            foreach (var argument in arguments) {
                start.ArgumentList.Add(argument);
            }
            start.Environment["DOTNET_ROOT"] = Path.GetDirectoryName(Dotnet);
            using var process = Process.Start(start)!;
            var error = process.StandardError.ReadToEndAsync();
            var text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return text + error.Result;
        }

        static IEnumerable<string> Roots() {
            if (Environment.GetEnvironmentVariable(Net8Root) is { Length: > 0 } root) {
                yield return root;
            }
            yield return Path.GetDirectoryName(Environment.ProcessPath)!;
        }

        static Net8Compiler? Of(string root) {
            var dotnet = Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            var csc = Net8Folders(Path.Combine(root, "sdk"))
                .Select(sdk => Path.Combine(sdk, "Roslyn", "bincore", "csc.dll"))
                .FirstOrDefault(File.Exists);
            var runtime = Net8Folders(Path.Combine(root, "shared", "Microsoft.NETCore.App")).FirstOrDefault();
            if (!File.Exists(dotnet) || csc is null || runtime is null) {
                return null;
            }
            string[] references = [Path.Combine(runtime, "System.Runtime.dll"), Path.Combine(runtime, "System.Private.CoreLib.dll")];
            return new Net8Compiler(dotnet, csc, references);
        }

        static IEnumerable<string> Net8Folders(string parent) =>
            Directory.Exists(parent) ? Directory.EnumerateDirectories(parent, "8.*").Order() : [];
    }
}
