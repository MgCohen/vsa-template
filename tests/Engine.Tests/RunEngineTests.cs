using Blocks;
using Build;
using Changes;
using Changes.Infrastructure;
using CodeCheck;
using Facts;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using Jobs;
using Jobs.Slices.BuildJob;
using Jobs.Slices.CheckJob;
using LoadApp;
using Model;
using Microsoft.Extensions.DependencyInjection;
using Operations;
using Operations.Contracts.Requests;
using Operations.Domain.Shared;
using Rules;
using Versions;
using Tests.Shared;
using Umpire.Engine.Versions;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Kernels;
using UmpireEngine;

namespace Engine.Tests;

public sealed class RunEngineTests {
    const string RulesSkipped =
        "umpire-engine: UMP030002: CheckRules skipped, because an earlier operation failed.";
    static readonly string Notes = Repo.Path("tests", "Fixtures", "Notes");

    [Test]
    public void The_scan_finds_each_module_of_the_one_engine_assembly() {
        var engine = typeof(OperationsPlugin).Assembly;

        var modules = new AppTypes(null).Modules.Where(module => module.GetType().Assembly == engine)
            .Select(module => module.Name);

        Assert.That(modules, Is.EqualTo(new[] {
            "Facts", "Operations", "Blocks", "Build", "Changes", "CodeCheck", "Jobs", "LoadApp", "Model", "Rules", "Versions" }));
    }

    [Test]
    public void The_analyzer_entry_gives_each_module_of_the_engine_assembly() {
        var engine = typeof(OperationsPlugin).Assembly;

        var modules = new AppTypes(null).Modules.Where(module => module.GetType().Assembly == engine).Select(module => module.Name);

        Assert.That(CompileHost.Modules().Select(module => module.Name), Is.EquivalentTo(modules));
    }

    [Test]
    public void The_engine_finds_the_definition_of_the_app_in_its_Definition_folder() {
        using var reader = new BinReader(AppContext.BaseDirectory);
        var bin = reader.Index();
        var folder = Repo.Path("engine");
        var entry = new BlockEntry(folder, EntryForm.App, null, folder);

        var result = LoadApp.Slices.LoadApp.LoadAppHandler.Definition(
            new Blocks.Domain.Shared.BlockTree(entry, []), bin.AppDlls(folder, []), bin.Definitions, new AppLoader(AppContext.BaseDirectory));

        Assert.That(result.Value.GetType().FullName, Is.EqualTo(typeof(EngineArchitecture).FullName));
    }

    [Test]
    public void An_app_class_that_implements_the_entry_for_two_kernels_gives_UMP030506() {
        var app = new AppClass(new TypeName("A.Shell", "A"), "A.cs", [new("A.First", "A"), new("A.Second", "A")], [], []);
        var dll = new BinDll("A.dll", "A", false, [], [], [], [app]);
        var entry = new BlockEntry("C:/a", EntryForm.App, null, "C:/a");

        var result = LoadApp.Slices.LoadApp.DefinitionSearch.Find(entry, [dll], []);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030506"));
        Assert.That(result.Error.Message, Is.EqualTo(
            "The app class A.Shell implements IAppEntry<TKernel> for more than one kernel: A.First, A.Second. Implement it for one kernel only."));
    }

    [Test]
    public void A_dll_with_no_app_class_has_no_app_class_in_the_index() {
        using var reader = new BinReader(AppContext.BaseDirectory);

        var shared = reader.Index().Dlls.Single(d => d.Name == typeof(Repo).Assembly.GetName().Name);

        Assert.That(shared.AppClasses, Is.Empty);
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_Notes() {
        var result = await Run();

        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.Contains(": error "));
    }

    [Test]
    public async Task An_Umpire_version_that_is_not_the_engine_version_gives_UMP030401_and_skips_the_load() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "Old.csproj"), """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Umpire.Vsa" Version="0.0.1" /></ItemGroup></Project>""");

            var result = await Run(block: root);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030401").And.Contains("Old.csproj: Umpire.Vsa 0.0.1"));
            Assert.That(result.Lines, Has.One.Contains("UMP030002: LoadApp skipped, because an earlier operation failed."));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase("", null, "warning")]
    [TestCase("umpire.check.mode = \"Error\"", null, "error")]
    [TestCase("umpire.check.mode = \"Error\"", ReportMode.Warn, "warning")]
    [TestCase("", ReportMode.Error, "error")]
    public async Task The_default_mode_is_Warn_and_the_key_or_the_flag_selects_Error(string config, ReportMode? mode, string severity) {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, ".umpireconfig"), config);
            File.WriteAllText(Path.Combine(root, "Old.csproj"), """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Umpire.Vsa" Version="0.0.1" /></ItemGroup></Project>""");

            var result = await Run(block: root, mode: mode);

            Assert.That(result.ExitCode, Is.EqualTo(severity == "error" ? 1 : 0));
            Assert.That(result.Lines, Has.One.Contains($"{severity} UMP030401"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task With_no_block_the_engine_uses_the_nearest_check_entry_above_the_project() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");
            var project = Directory.CreateDirectory(Path.Combine(root, "App", "Host")).FullName;

            var result = await Run(project: project);

            Assert.That(result.Lines, Has.One.Contains("error UMP030501").And.Contains($"under the block folder {Path.GetFullPath(root)}"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task With_no_check_entry_and_no_block_the_run_gives_UMP030303_and_runs_no_other_step() {
        var project = TempFolder.Create();
        try {
            var result = await Run(project: project, bin: project);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030303: No check entry is in"));
            Assert.That(result.Lines, Has.None.Contains("UMP030002"));
        } finally {
            TempFolder.Delete(project);
        }
    }

    [TestCase("packs", "Vsa")]
    [TestCase("tests", "Fixtures", "Blocks", "App", "Packs", "Alpha")]
    [TestCase("tests", "Fixtures", "PackVariant", "App", "Packs", "Ecs")]
    public async Task A_pack_block_passes_its_own_check(params string[] folder) {
        var result = await Run(project: Repo.Path(folder));

        Assert.That(result.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task An_unmarked_block_in_a_part_folder_gives_UMP020401() {
        using var notes = FixtureLock.Take("Notes");
        var store = Path.Combine(Notes, "App", "Features", "Notebook", "Infrastructure", "Store");
        Directory.CreateDirectory(store);
        try {
            File.WriteAllText(Path.Combine(store, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");

            var result = await Run(project: Notes);

            Assert.That(result.Lines, Has.One.Contains("error UMP020401: Infrastructure (App/Features/{feature}/Infrastructure) covers "
                + "the folder App/Features/Notebook/Infrastructure/Store"));
        } finally {
            Directory.Delete(store, recursive: true);
        }
    }

    [Test]
    public async Task A_severity_of_none_in_the_umpireconfig_of_the_block_turns_a_rule_off() {
        using var notes = FixtureLock.Take("Notes");
        var store = Path.Combine(Notes, "App", "Features", "Notebook", "Infrastructure", "Store");
        var config = Path.Combine(Notes, ".umpireconfig");
        var original = File.ReadAllText(config);
        Directory.CreateDirectory(store);
        try {
            File.WriteAllText(Path.Combine(store, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");
            File.WriteAllText(config, original + "umpire.UMP020401.severity = \"none\"\n");

            var result = await Run(project: Notes);

            Assert.That(result.Lines, Has.None.Contains("UMP020401").And.None.Contains("UMP030001"), string.Join("\n", result.Lines));
        } finally {
            File.WriteAllText(config, original);
            Directory.Delete(store, recursive: true);
        }
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_a_block_with_inner_blocks() {
        var result = await Run(project: Repo.Path("tests", "Fixtures", "Blocks"));

        Assert.That(result.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_an_app_with_its_own_pack_variant() {
        var result = await Run(project: Repo.Path("tests", "Fixtures", "PackVariant"));

        Assert.That(result.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task A_step_that_fails_skips_the_steps_that_use_its_output() {
        var result = await Run(block: Path.Combine(Notes, "App", "Features", "Missing"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030501"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_with_two_app_classes_gives_UMP030503_with_both_names() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "TwoApps"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030503: The app has more than one app class: "
            + "TwoApps.Host.FirstApp, TwoApps.Host.SecondApp."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task A_launcher_entry_with_a_private_app_class_and_no_empty_constructor_loads_its_architecture() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "ShellApp"));

        string[] loadAppCodes = [
            LoadApp.Slices.LoadApp.LoadAppCodes.NoAppAssembly, LoadApp.Slices.LoadApp.LoadAppCodes.NoAppClass,
            LoadApp.Slices.LoadApp.LoadAppCodes.ManyAppClasses, LoadApp.Slices.LoadApp.LoadAppCodes.ManyKernels,
        ];
        Assert.That(result.Lines.Where(line => loadAppCodes.Any(line.Contains)), Is.Empty, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.EqualTo(RulesSkipped), string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task An_app_class_with_an_open_host_loads_its_architecture() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "OpenHost"));

        string[] loadAppCodes = [
            LoadApp.Slices.LoadApp.LoadAppCodes.NoAppAssembly, LoadApp.Slices.LoadApp.LoadAppCodes.NoAppClass,
            LoadApp.Slices.LoadApp.LoadAppCodes.ManyAppClasses, LoadApp.Slices.LoadApp.LoadAppCodes.ManyKernels,
        ];
        Assert.That(result.Lines.Where(line => loadAppCodes.Any(line.Contains)), Is.Empty, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.EqualTo(RulesSkipped), string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
    }

    [TestCase(null, 2)]
    [TestCase(false, 0)]
    public async Task The_project_checks_flag_stops_the_project_warnings(bool? projectChecks, int warnings) {
        var block = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(block, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");
            File.WriteAllText(Path.Combine(block, "First.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(block, "Second.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            var result = await Run(project: block, projectChecks: projectChecks);

            Assert.That(result.Lines.Count(line => line.Contains("UMP030305")), Is.EqualTo(warnings), string.Join("\n", result.Lines));
        } finally {
            TempFolder.Delete(block);
        }
    }

    [Test]
    public async Task An_app_with_no_app_class_gives_UMP030502() {
        var result = await Run(block: Repo.Path("tests", "Shared"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030502: The app has no app class."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task A_step_that_throws_gives_one_error_and_the_run_continues() {
        var result = await Run(bin: Path.Combine(Notes, "missing-bin"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.StartsWith("umpire-engine: error UMP030001: LoadApp could not run: "));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public void With_fail_fast_a_step_that_throws_stops_the_run() {
        var bin = Path.Combine(Notes, "missing-bin");

        Assert.ThrowsAsync<DirectoryNotFoundException>(() => Run(bin: bin, failFast: true));
    }

    [Test]
    public async Task A_umpireconfig_file_that_is_not_TOML_stops_the_run_with_the_line_of_the_error() {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, ".umpireconfig");
            File.WriteAllText(file, "umpire.UMP010101.max = 60\n[*.cs]\n");

            var result = await Run(block: root);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.StartsWith("umpire-engine: error UMP030001: FindBlocks could not run: ")
                .And.Contains($"{Path.GetFullPath(file)}(2,"));
            Assert.That(result.Lines, Has.None.Contains("UMP030002"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    internal static Module[] EngineModules() => [
        new OperationsPlugin(), new FactsPlugin(), new JobsModule(), new BuildModule(), new ChangesModule(), new BlocksModule(),
        new VersionsModule(), new LoadAppModule(), new RulesModule(), new ModelModule(), new CodeCheckModule(),
    ];

    internal static async Task<ConsoleOutput> Run(
        string? block = null, string? bin = null, bool failFast = false, string? project = null,
        bool build = false, bool keepStamp = false, bool? projectChecks = null,
        ReportMode? mode = ReportMode.Error, string ignoredFolders = "") =>
        ConsoleReport.Of(await Report(block, bin, failFast, project, build, keepStamp, projectChecks, mode, ignoredFolders));

    internal static async Task<JobReport> Report(
        string? block = null, string? bin = null, bool failFast = false, string? project = null,
        bool build = false, bool keepStamp = false, bool? projectChecks = null,
        ReportMode? mode = ReportMode.Error, string ignoredFolders = "") {
        using var host = new EngineTestApp(EngineModules()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var declared = project is null ? block ?? Notes : "";
        var output = bin ?? AppContext.BaseDirectory;
        if (Directory.Exists(output) && keepStamp is false) {
            StampFile.Delete(output);
        }
        var requests = scope.ServiceProvider.GetRequiredService<IRequestSender>();
        if (build) {
            var check = new CheckJobRequest(
                project ?? declared, output, declared, mode, FailFast: failFast, IgnoredFolders: ignoredFolders,
                ProjectChecks: projectChecks);
            return (await requests.SendAsync(check, default)).Value;
        }
        var request = new BuildJobRequest(
            project ?? declared, output, declared, mode, FailFast: failFast, IgnoredFolders: ignoredFolders,
            ProjectChecks: projectChecks);
        return (await requests.SendAsync(request, default)).Value;
    }
}
