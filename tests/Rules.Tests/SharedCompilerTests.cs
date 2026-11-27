using System.Reflection;
using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class SharedCompilerTests {
    static readonly string Configuration =
        typeof(SharedCompilerTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;

    static readonly string[] EntryFiles = ["Umpire.Engine.Analyzer.dll", "Umpire.Engine.dll", "Umpire.dll", "Umpire.Vsa.dll",
        "Microsoft.Extensions.DependencyInjection.dll"];

    [Test]
    public void A_source_mode_app_gives_each_analyzer_file_from_the_folder_of_the_analyzer_entry() {
        var probe = TempFolder.Create();
        try {
            WriteProbe(probe);

            var result = Dotnet.Run(probe, null, "msbuild", "Probe.csproj", "-restore", "-nodeReuse:false",
                "-t:ResolveProjectReferences", "-p:BuildProjectReferences=false", $"-p:Configuration={Configuration}",
                "-getItem:Analyzer").EnsureSuccess();
            var analyzers = result.Output.Split('\n')
                .Where(line => line.Contains("\"Identity\"", StringComparison.Ordinal))
                .Select(line => line.Split('"')[3])
                .ToList();

            Assert.That(analyzers.Select(Path.GetFileName), Is.SupersetOf(EntryFiles).And.No.Member("Microsoft.CodeAnalysis.dll"), result.Output);
            Assert.That(analyzers.Where(path => EntryFiles.Contains(Path.GetFileName(path))).Select(Path.GetDirectoryName).Distinct(),
                Has.Exactly(1).Items, result.Output);
        } finally {
            TempFolder.Delete(probe);
        }
    }

    [Test]
    public void A_deleted_copy_of_an_analyzer_file_on_the_compiler_server_does_not_stop_the_rules() {
        var server = new Dictionary<string, string> { ["SharedCompilationId"] = "umpire-test-" + Guid.NewGuid().ToString("N") };
        var decoy = Repo.Path("artifacts~", "0-decoy-" + Guid.NewGuid().ToString("N")[..8]);
        var decoyProject = TempFolder.Create();
        var probe = TempFolder.Create();
        try {
            Directory.CreateDirectory(decoy);
            var decoyAnalyzer = Path.Combine(decoy, "Umpire.Engine.dll");
            File.Copy(typeof(UmpireEngine.EngineApp<>).Assembly.Location, decoyAnalyzer);
            WriteDecoyProject(decoyProject, decoyAnalyzer);
            Dotnet.Run(decoyProject, server, "build", "Decoy.csproj", "-nodeReuse:false").EnsureSuccess();
            TempFolder.Delete(decoy);
            WriteProbe(probe);

            var result = Dotnet.Run(probe, server, "msbuild", "Probe.csproj", "-restore", "-nodeReuse:false",
                "-t:Compile", "-p:BuildProjectReferences=false", $"-p:Configuration={Configuration}", "-p:ReportAnalyzer=true", "-v:d");

            Assert.That(new[] { "error CS8032", "warning CS8032", "error AD0001", "warning AD0001" }.Where(result.Output.Contains), Is.Empty, result.Output);
            Assert.That(result.Succeeded, Is.True, result.Output);
            Assert.That(result.Output, Does.Contain("UmpireEngine.EngineAnalyzer ("), result.Output);
        } finally {
            StopServer(probe, server);
            TempFolder.Delete(decoy);
            TempFolder.Delete(decoyProject);
            TempFolder.Delete(probe);
        }
    }

    static void StopServer(string folder, IReadOnlyDictionary<string, string> server) {
        var roslyn = Dotnet.Run(folder, null, "msbuild", "-getProperty:RoslynTargetsPath",
            Repo.Path("tests", "Shared", "Tests.Shared.csproj")).Output.Trim();
        var compilerServer = Path.Combine(roslyn, "bincore", "VBCSCompiler.dll");
        if (File.Exists(compilerServer)) {
            Dotnet.Run(folder, null, "exec", compilerServer, "-shutdown", $"-pipename:{server["SharedCompilationId"]}");
        }
    }

    static void WriteDecoyProject(string folder, string analyzer) {
        File.WriteAllText(Path.Combine(folder, "Decoy.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Analyzer Include="{analyzer}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(folder, "Decoy.cs"), "namespace Decoy;\n\ninternal static class Part;\n");
    }

    static void WriteProbe(string folder) {
        File.WriteAllText(Path.Combine(folder, "Directory.Build.props"), $"""
            <Project>
              <Import Project="{Repo.Path("Directory.Build.props")}" />
              <PropertyGroup>
                <UmpireFrameworkSourceDir>$([MSBuild]::NormalizeDirectory('{Repo.Root}'))</UmpireFrameworkSourceDir>
                <UmpireCheck>false</UmpireCheck>
                <UmpireBlock>$(MSBuildThisFileDirectory)</UmpireBlock>
                <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts~</ArtifactsPath>
              </PropertyGroup>
              <Import Project="{Repo.Path("tools", "bundle", "SourceMode", "Umpire.SourceMode.props")}" />
            </Project>
            """);
        File.WriteAllText(Path.Combine(folder, "Probe.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Umpire.Vsa" Version="$(UmpireVersion)" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(folder, "Part.cs"), "namespace Probe;\n\ninternal static class Part;\n");
    }
}
