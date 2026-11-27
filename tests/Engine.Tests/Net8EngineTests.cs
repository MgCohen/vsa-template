using System.Diagnostics;
using Tests.Shared;

namespace Engine.Tests;

[NonParallelizable]
public sealed class Net8EngineTests {
    const string Net8Root = "UMPIRE_NET8_DOTNET_ROOT";

    [Test]
    public void The_engine_checks_its_own_block_on_a_dotnet_with_only_the_net8_runtime() {
        var dotnet = Net8Dotnet.Find() ?? throw new IgnoreException(
            $"No dotnet folder with only the .NET 8 runtime. Set {Net8Root} to such a folder, "
            + "for example the one in a Unity 6000.6 editor.");
        var bin = TempFolder.Create();
        try {
            TempFolder.Copy(EngineBin(), bin);
            TempFolder.Delete(Repo.Path("engine", "artifacts~", "umpire-checks"));

            var run = dotnet.Run("exec", Path.Combine(bin, "umpire-engine.dll"), "jobs", "build",
                "--project", Repo.Path("engine"), "--block", Repo.Path("engine"), "--bin", bin);

            Assert.That(run.ExitCode, Is.EqualTo(0), run.Output);
            Assert.That(run.Output, Does.Contain("umpire-engine: 0 errors").And.Not.Contain("UMP030201")
                .And.Not.Contain($"The block {Repo.Path("engine")} did not change"), run.Output);
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [Test]
    public void The_engine_needs_only_the_dotnet_runtime_and_no_other_shared_framework() {
        var config = File.ReadAllText(Path.Combine(EngineBin(), "umpire-engine.runtimeconfig.json"));

        Assert.That(config, Does.Contain("\"Microsoft.NETCore.App\"").And.Not.Contain("Microsoft.AspNetCore.App"), config);
    }

    static string EngineBin() {
        var configuration = Path.GetFileName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Repo.Path("engine", "artifacts~", "bin", "Engine", configuration);
    }

    sealed record Net8Dotnet(string Executable) {
        public static Net8Dotnet? Find() {
            var root = Environment.GetEnvironmentVariable(Net8Root);
            if (root is not { Length: > 0 }) {
                return null;
            }
            var executable = Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            var runtimes = Path.Combine(root, "shared", "Microsoft.NETCore.App");
            var onlyNet8 = Directory.Exists(runtimes)
                && Directory.EnumerateDirectories(runtimes).Select(Path.GetFileName).All(version => version!.StartsWith("8.", StringComparison.Ordinal));
            return File.Exists(executable) && onlyNet8 ? new Net8Dotnet(executable) : null;
        }

        public DotnetResult Run(params string[] arguments) {
            var start = new ProcessStartInfo(Executable) {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var argument in arguments) {
                start.ArgumentList.Add(argument);
            }
            start.Environment["DOTNET_ROOT"] = Path.GetDirectoryName(Executable);
            start.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            using var process = Process.Start(start)!;
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return new DotnetResult(string.Join(' ', arguments), process.ExitCode, output + error.Result);
        }
    }
}
