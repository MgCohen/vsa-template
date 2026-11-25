using Blocks.Domain;
using Blocks.Infrastructure;
using Blocks.Slices.FindBlocks;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Steps.Domain.Shared;
using Tests.Shared;
using UmpireEngine;

namespace Engine.Tests;

public sealed class BlocksTests {
    const string PackArchitecture = "Umpire.Kernels.PackArchitecture, Umpire.Kernels";
    const string VsaArchitecture = "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa";
    static readonly BinIndex NoBin = new("", [], []);
    static readonly string Fixture = Repo.Path("tests", "Fixtures", "Blocks");
    static BinIndex? index;

    static BinIndex Index {
        get {
            if (index is null) {
                using var reader = new BinReader(AppContext.BaseDirectory);
                index = reader.Index();
            }
            return index;
        }
    }

    static IReadOnlyList<BlockEntry> Entries => Index.Entries;

    [Test]
    public void An_app_class_and_a_definition_are_entries_of_the_block_folder() {
        Assert.That(EntriesIn(Fixture), Is.EquivalentTo(new[] { EntryForm.App, EntryForm.Definition }));
    }

    [TestCase("Alpha")]
    [TestCase("Beta")]
    public void The_definition_of_a_pack_is_the_check_entry_of_the_pack_folder(string pack) {
        var entry = Entries.Single(e => Same(e.Folder, Path.Combine(Fixture, "App", "Packs", pack)));

        Assert.That(entry.Form, Is.EqualTo(EntryForm.Definition));
        Assert.That(entry.Type, Is.EqualTo(TypeName.Parse(PackArchitecture)));
    }

    [Test]
    public void An_app_with_its_own_pack_has_the_app_entry_and_the_pack_entry() {
        var app = Repo.Path("tests", "Fixtures", "PackVariant");

        Assert.That(EntriesIn(app), Is.EqualTo(new[] { EntryForm.App }));
        Assert.That(EntriesIn(Path.Combine(app, "App", "Packs", "Ecs")), Is.EqualTo(new[] { EntryForm.Definition }));
    }

    [TestCase("packs/Vsa", "Umpire.Vsa.Definition.VsaArchitecture")]
    [TestCase("tests/Fixtures/PackVariant/App/Packs/Ecs", "PackVariant.Packs.Ecs.Definition.EcsArchitecture")]
    public void A_pack_entry_names_the_architecture_of_the_pack(string folder, string architecture) {
        var packed = Entries.Where(e => Same(e.Folder, Repo.Path(folder.Split('/'))) && e.Packed is not null)
            .Select(e => e.Packed!.FullName).Distinct();

        Assert.That(packed, Is.EqualTo(new[] { architecture }));
    }

    [Test]
    public void A_definition_in_an_app_block_is_no_second_check_entry() {
        var result = BlockSearch.Find(
            "C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), Pack("C:/a", "A.MyArchitecture")], NoBin);

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.App));
        Assert.That(result.Value.Findings, Is.Empty);
    }

    [Test]
    public void A_definition_with_no_app_class_is_a_pack() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Pack("C:/a", "A.MyArchitecture")], NoBin);

        Assert.That(result.Value.Blocks.Current.Packed, Is.EqualTo(new TypeName("A.MyArchitecture", "A")));
    }

    [Test]
    public void A_check_entry_that_names_the_definition_is_the_check_entry() {
        var definition = Entry("C:/a", EntryForm.Metadata);

        var result = BlockSearch.Find(
            "C:/a", Path.GetFullPath("C:/a"), [Pack("C:/a", definition.Type!.FullName, "Umpire.Vsa"), definition], NoBin);

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.Metadata));
    }

    [Test]
    public void A_definition_and_a_check_entry_of_another_architecture_give_UMP030301() {
        var result = BlockSearch.Find(
            "C:/a", Path.GetFullPath("C:/a"), [Pack("C:/a", "A.MyArchitecture"), Entry("C:/a", EntryForm.Metadata)], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030301"));
    }

    [TestCase(EntryForm.Metadata, 0)]
    [TestCase(EntryForm.File, 1)]
    public void The_definition_of_an_app_block_wins_over_a_check_entry(EntryForm form, int warnings) {
        BlockEntry[] entries = [Entry("C:/a", EntryForm.App), Pack("C:/a", "A.MyArchitecture"), Entry("C:/a", form)];

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), entries, NoBin);

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.App));
        Assert.That(result.Value.Findings.Count(f => f.Code == "UMP030304"), Is.EqualTo(warnings));
    }

    [Test]
    public void A_definition_with_no_kernel_of_an_app_block_wins_over_a_check_entry() {
        var definition = new DefinitionType(new TypeName("A.MyArchitecture", "A"), Path.GetFullPath("C:/a"), null, []);
        var dll = new BinDll("A.dll", "A", false, [], [], [], []) { Definitions = [definition] };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"),
            [Entry("C:/a", EntryForm.App), Entry("C:/a", EntryForm.Metadata)], new BinIndex("", [dll], []));

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.App));
    }

    [TestCase("C:/a/App/Host", true)]
    [TestCase("C:/a/Samples", false)]
    public void A_block_whose_app_class_has_another_entry_folder_is_an_app(string entryFolder, bool isApp) {
        var source = Path.GetFullPath(entryFolder + (isApp ? "/MyApp.cs" : "/App/MyApp.cs"));
        var app = new AppClass(new TypeName("A.App", "A"), source, [new TypeName("A.Kernel", "A")], [], []);
        var dll = new BinDll("A.dll", "A", false, [], [], [], [app]);
        var entry = new BlockEntry(Path.GetFullPath(entryFolder), EntryForm.App, app.Type, source) { HasProject = !isApp };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Pack("C:/a", "A.MyArchitecture"), entry],
            new BinIndex("", [dll], []));

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(isApp ? EntryForm.App : EntryForm.Definition));
    }

    [Test]
    public void A_check_entry_of_an_app_block_with_no_definition_names_its_architecture() {
        var result = BlockSearch.Find(
            "C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), Entry("C:/a", EntryForm.File)], NoBin);

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.File));
        Assert.That(result.Value.Blocks.Current.HasApp, Is.True);
    }

    [Test]
    public void The_engine_reads_the_definitions_in_the_definitions_folder_of_the_bin() {
        var bin = TempFolder.Create();
        try {
            var definitions = Directory.CreateDirectory(BinReader.DefinitionsFolder(bin)).FullName;
            foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "Umpire.*.dll")) {
                File.Copy(file, Path.Combine(bin, Path.GetFileName(file)));
            }
            foreach (var name in new[] { "Engine.Definition.dll", "Engine.Definition.pdb" }) {
                File.Copy(Path.Combine(AppContext.BaseDirectory, name), Path.Combine(definitions, name));
            }

            using var reader = new BinReader(bin);
            var found = reader.Index().Definitions.Single(d => d.Type.Assembly == "Engine.Definition");
            var created = new AppLoader(bin).CreateArchitecture(found.Type);

            Assert.That(found.Folder, Is.EqualTo(Repo.Path("engine")));
            Assert.That(found.Kernel?.FullName, Is.EqualTo("Umpire.Vsa.Composition.VsaKernel"));
            Assert.That(created.GetType().FullName, Is.EqualTo(typeof(EngineArchitecture).FullName));
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [Test]
    public void A_definition_whose_folder_is_an_alias_of_the_block_is_in_the_block() {
        var block = Path.GetFullPath("C:/a");
        var aliases = new FolderAliasConfig([new Umpire.Architecture.FolderAlias("Assets/Game", "")], []);
        var game = Path.Combine(block, "Assets", "Game");
        var definition = new DefinitionType(new TypeName("A.MyArchitecture", "A"), game, null, []);
        var dll = new BinDll("A.dll", "A", false, [], [], [], []) { Definitions = [definition] };

        var bin = new BinIndex("", [dll], [Pack(game, "A.MyArchitecture")])
            .WithDefinitionFolders(folder => aliases.DefinitionBlock(block, folder));

        Assert.That(bin.Definitions.Single().Folder, Is.EqualTo(block));
        Assert.That(bin.Entries.Single().Folder, Is.EqualTo(block));
    }

    [Test]
    public void Assembly_metadata_with_Umpire_Root_is_the_check_entry_of_the_folder_above_the_project() {
        var entry = Entries.Single(e => Same(e.Folder, Path.Combine(Fixture, "App", "Lib")));

        Assert.That(entry.Form, Is.EqualTo(EntryForm.Metadata));
        Assert.That(entry.Type, Is.EqualTo(TypeName.Parse(VsaArchitecture)));
        Assert.That(entry.Source, Does.EndWith("Lib.csproj"));
    }

    [Test]
    public void An_abstract_architecture_base_is_no_check_entry() {
        Assert.That(EntriesIn(Repo.Path("framework", "Kernel", "App")), Is.Empty);
    }

    [Test]
    public void The_VSA_pack_has_one_check_entry() {
        var vsa = Entries.Where(e => Same(e.Folder, Repo.Path("packs", "Vsa"))).ToList();

        Assert.That(vsa.Select(e => e.Key).Distinct(), Is.EqualTo(new[] { $"Definition: {VsaArchitecture}" }));
    }

    [TestCase("Lib.Missing, Umpire.Vsa", "Umpire.Vsa has no type Lib.Missing")]
    [TestCase("Umpire.Kernels.Kernel, Umpire.Kernels", "does not inherit Umpire.Architecture.Architecture")]
    [TestCase("Some.Architecture, Some.Missing", "the build output has no assembly Some.Missing")]
    public void A_bad_architecture_name_gives_its_reason(string name, string reason) {
        using var reader = new BinReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem(TypeName.Parse(name)!), Does.Contain(reason));
    }

    [Test]
    public void A_name_with_no_assembly_is_no_type_name() {
        Assert.That(TypeName.Parse("Umpire.Vsa.Definition.VsaArchitecture"), Is.Null);
    }

    [Test]
    public void A_good_architecture_name_gives_no_problem() {
        using var reader = new BinReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem(TypeName.Parse(VsaArchitecture)!), Is.Null);
    }

    [Test]
    public void One_app_class_check_finds_the_engine_app_class_with_its_bases() {
        var app = Index.Dlls.Single(d => d.Name == "umpire-engine").AppClasses.Single();

        Assert.That(app.Type, Is.EqualTo(new TypeName(typeof(EngineApp).FullName!, "umpire-engine")));
        Assert.That(app.Bases, Does.Contain("Umpire.Kernels.App"));
        Assert.That(app.Kernels.Select(k => k.FullName), Is.EqualTo(new[] { "Umpire.Vsa.Composition.VsaKernel" }));
        Assert.That(app.KernelBases.Select(k => k.FullName), Does.Contain("Umpire.Kernels.Kernel"));
    }

    [Test]
    public void The_leaves_of_the_app_classes_drop_each_base_app_class() {
        TypeName[] kernel = [new("A.Kernel", "A")];
        var first = new AppClass(new TypeName("A.First", "A"), "A.cs", kernel, [], ["Umpire.Kernels.App"]);
        var second = new AppClass(new TypeName("A.Second", "A"), "B.cs", kernel, [], ["A.First", "Umpire.Kernels.App"]);

        Assert.That(AppClass.Leaves([first, second]), Is.EqualTo(new[] { second }));
    }

    [Test]
    public void The_leaves_drop_an_app_class_nested_in_an_entry_with_the_same_kernel() {
        TypeName[] kernel = [new("A.Kernel", "A")];
        var shell = new AppClass(new TypeName("A.GameShell", "A"), "A.cs", kernel, [], []);
        var inner = new AppClass(new TypeName("A.GameShell+Inner", "A"), "A.cs", kernel, [], ["Umpire.Kernels.App"]);

        Assert.That(AppClass.Leaves([shell, inner]), Is.EqualTo(new[] { shell }));
    }

    [Test]
    public void The_leaves_keep_a_nested_app_class_with_another_kernel() {
        var shell = new AppClass(new TypeName("A.GameShell", "A"), "A.cs", [new("A.Kernel", "A")], [], []);
        var inner = new AppClass(new TypeName("A.GameShell+Inner", "A"), "A.cs", [new("A.Other", "A")], [], ["Umpire.Kernels.App"]);

        Assert.That(AppClass.Leaves([shell, inner]), Has.Count.EqualTo(2));
    }

    [Test]
    public void The_launcher_of_the_shell_fixture_is_the_one_leaf_with_its_kernel() {
        var shell = Index.Dlls.Single(d => d.Name == "ShellApp");

        var leaf = AppClass.Leaves(shell.AppClasses).Single();

        Assert.That(leaf.Type.FullName, Is.EqualTo("ShellApp.Host.GameLauncher"));
        Assert.That(leaf.Kernels.Single().FullName, Is.EqualTo("Umpire.Vsa.Composition.VsaKernel"));
    }

    [Test]
    public void The_source_folder_of_a_library_skips_the_generated_files_but_not_a_parent_folder_named_bin() {
        var sources = new[] { "C:/x/bin/Lib/A.cs", "C:/x/bin/Lib/Sub/B.cs", "C:/x/bin/artifacts~/obj/Lib/G.cs" }
            .Select(file => new Umpire.Architecture.Source(file, 1));

        Assert.That(BinReader.SourceFolder(sources), Is.EqualTo(Path.GetFullPath("C:/x/bin/Lib")));
    }

    [Test]
    public void A_skipped_assembly_is_not_a_referenced_dll() {
        var app = Index.AppDlls(Fixture, ["Lib"]);

        var names = (IReadOnlyList<string> skip) => Index.ReferencedDlls(app, skip).Select(d => d.Name).ToList();

        Assert.That(app.Select(d => d.Name), Does.Contain("Blocks").And.Not.Contain("Lib"));
        Assert.That(names([]), Does.Contain("Lib"));
        Assert.That(names(["Lib"]), Does.Not.Contain("Lib"));
    }

    [Test]
    public void The_nearest_entry_above_the_project_is_the_block() {
        var entries = new[] { Entry("C:/a", EntryForm.File), Entry("C:/a/b", EntryForm.App) };

        Assert.That(BlockSearch.Folder("C:/a/b/c", "", entries), Is.EqualTo(Path.GetFullPath("C:/a/b")));
        Assert.That(BlockSearch.Folder("C:/a/x", "", entries), Is.EqualTo(Path.GetFullPath("C:/a")));
        Assert.That(BlockSearch.Folder("C:/z", "", entries), Is.Null);
        Assert.That(BlockSearch.Folder("C:/a/b/c", "C:/d", entries), Is.EqualTo(Path.GetFullPath("C:/d")));
    }

    [Test]
    public void No_block_gives_UMP030303() {
        var result = BlockSearch.Find("C:/z", null, [], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030303"));
    }

    [Test]
    public void Two_entries_in_the_block_give_UMP030301_and_stop_the_run() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.Base), Entry("C:/a", EntryForm.Metadata)], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030301"));
        Assert.That(result.Error.Message, Does.Contain("more than one check entry"));
    }

    [Test]
    public void Two_entries_in_an_inner_block_give_UMP030301_and_the_run_continues() {
        var inner = new[] { Entry("C:/a/t", EntryForm.Base), Entry("C:/a/t", EntryForm.Metadata) };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), .. inner], NoBin);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "UMP030301" }));
        Assert.That(result.Value.Blocks.InnerFolders, Is.EqualTo(new[] { Path.GetFullPath("C:/a/t") }));
    }

    [Test]
    public void Two_entry_forms_in_one_file_give_UMP030301() {
        var app = Entry("C:/a", EntryForm.Metadata);
        var typed = app with { Form = EntryForm.Base };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [app, typed], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030301"));
    }

    [Test]
    public void An_entry_in_code_wins_over_the_key_in_the_file_with_UMP030304() {
        var file = Entry("C:/a", EntryForm.File);

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.Base), file], NoBin);

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.Base));
        Assert.That(result.Value.Findings.Single().Code, Is.EqualTo("UMP030304"));
        Assert.That(result.Value.Findings.Single().Severity, Is.EqualTo(FindingSeverity.Warning));
        Assert.That(result.Value.Findings.Single().File, Is.EqualTo(file.Source));
    }

    [Test]
    public void An_entry_in_code_of_an_inner_block_wins_over_its_key_with_UMP030304() {
        var inner = new[] { Entry("C:/a/t", EntryForm.Base), Entry("C:/a/t", EntryForm.File) };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), .. inner], NoBin);

        Assert.That(result.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "UMP030304" }));
        Assert.That(result.Value.Blocks.Inner, Is.EqualTo(new[] { inner[0] }));
    }

    [Test]
    public void A_project_with_no_Umpire_main_SDK_gives_UMP030305() {
        var projects = new[] { new BlockProject("C:/a/A.csproj", true), new BlockProject("C:/a/B/B.csproj", false) };

        var findings = ProjectCheck.Findings("C:/a", projects, hasConfigFile: true);

        Assert.That(findings.Select(f => $"{f.Code} {f.File} {f.Severity}"),
            Is.EqualTo(new[] { "UMP030305 C:/a/B/B.csproj Warning" }));
    }

    [Test]
    public void A_block_with_two_projects_and_no_umpireconfig_file_gives_UMP030306() {
        var projects = new[] { new BlockProject("C:/a/A.csproj", true), new BlockProject("C:/a/B/B.csproj", true) };

        Assert.That(ProjectCheck.Findings("C:/a", projects, hasConfigFile: false).Select(f => f.Code),
            Is.EqualTo(new[] { "UMP030306" }));
        Assert.That(ProjectCheck.Findings("C:/a", projects, hasConfigFile: true), Is.Empty);
        Assert.That(ProjectCheck.Findings("C:/a", [projects[0]], hasConfigFile: false), Is.Empty);
    }

    [Test]
    public void The_projects_of_a_block_skip_the_inner_blocks_and_the_build_output() {
        var root = TempFolder.Create();
        try {
            WriteFile(root, "App.csproj", """<Project Sdk="Umpire.Sdk.Web/0.0.1" />""");
            WriteFile(root, "Tests/App.Tests.csproj", "<Project>\n  <Import Project=\"$([MSBuild]::GetPathOfFileAbove('Umpire.Sdk.Dev.props'))\" />\n</Project>");
            WriteFile(root, "Tools/Tool.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");
            WriteFile(root, "Inner/Inner.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");
            WriteFile(root, "artifacts~/Copy.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");
            var full = Path.GetFullPath(root);

            var projects = BlockProjects.In(full, [Path.Combine(full, "Inner")]);

            Assert.That(projects.Select(p => $"{Path.GetRelativePath(full, p.File).Replace('\\', '/')} {p.HasUmpireSdk}"),
                Is.EqualTo(new[] { "App.csproj True", "Tests/App.Tests.csproj True", "Tools/Tool.csproj False" }));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void An_architecture_with_no_empty_constructor_gives_its_reason() {
        using var reader = new BinReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem(new TypeName(typeof(NamedArchitecture).FullName!, "Engine.Tests")),
            Does.Contain("has no public constructor with no parameters"));
    }

    [Test]
    public void A_bad_name_in_an_inner_block_gives_UMP030302() {
        var bad = Entry("C:/a/t", EntryForm.File).Names("X", TypeName.NoAssembly);

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), bad], NoBin);

        Assert.That(result.Value.Findings.Single().Code, Is.EqualTo("UMP030302"));
        Assert.That(result.Value.Findings.Single().Message, Does.Contain("for example \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\""));
    }

    [Test]
    public void One_entry_that_is_found_two_times_is_one_entry() {
        var file = Entry("C:/a/t", EntryForm.File);

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), file, file with { }], NoBin);

        Assert.That(result.Value.Findings, Is.Empty);
        Assert.That(result.Value.Blocks.Inner, Is.EqualTo(new[] { file }));
    }

    [Test]
    public void A_declared_folder_with_no_entry_is_an_app_block() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [], NoBin);

        Assert.That(result.Value.Blocks.Current.Form, Is.EqualTo(EntryForm.App));
    }

    [Test]
    public void The_search_for_entry_files_skips_the_build_output() {
        var root = TempFolder.Create();
        try {
            foreach (var folder in new[] { "Tools", "bin", "obj", "artifacts~", Path.Combine("Tools", "obj") }) {
                Directory.CreateDirectory(Path.Combine(root, folder));
                File.WriteAllText(Path.Combine(root, folder, EntryFiles.Name), $"umpire.architecture = \"{VsaArchitecture}\"");
            }

            var found = EntryFiles.Below(root);

            Assert.That(found.Select(f => f.Folder), Is.EqualTo(new[] { Path.Combine(Path.GetFullPath(root), "Tools") }));
            Assert.That(found.Single().Type, Is.EqualTo(TypeName.Parse(VsaArchitecture)));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase("umpire.architecture = \"\"", "does not name an architecture: the key umpire.architecture has no value")]
    [TestCase("umpire.architecture = \"Lib.X\"", "names \"Lib.X\", but the name has no assembly")]
    public void A_bad_entry_file_gives_its_reason(string text, string reason) {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, EntryFiles.Name);
            File.WriteAllText(file, text);

            Assert.That(EntryFiles.Read(file)!.Problem, Does.StartWith(reason));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase("")]
    [TestCase("umpire.UMP010101.max = 140")]
    [TestCase("[Tools]\numpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"")]
    public void An_umpireconfig_file_with_no_top_level_architecture_key_is_no_entry(string text) {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, EntryFiles.Name);
            File.WriteAllText(file, text);

            Assert.That(EntryFiles.Read(file), Is.Null);
            Assert.That(EntryFiles.Above(root), Is.Empty);
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void The_Tools_fixture_names_its_architecture_with_the_key() {
        var entry = EntryFiles.Read(Path.Combine(Fixture, "App", "Tools", EntryFiles.Name))!;

        Assert.That(entry.Form, Is.EqualTo(EntryForm.File));
        Assert.That(entry.Type, Is.EqualTo(TypeName.Parse(VsaArchitecture)));
    }

    [Test]
    public void A_native_dll_in_the_bin_is_not_in_the_index_and_stays_free() {
        var bin = TempFolder.Create();
        try {
            var native = Path.Combine(bin, "native.dll");
            File.Copy(NativeRuntimeFile(), native);
            File.Copy(typeof(Repo).Assembly.Location, Path.Combine(bin, "Tests.Shared.dll"));

            using (var reader = new BinReader(bin)) {
                Assert.That(reader.Index().Dlls.Select(d => d.Name), Is.EqualTo(new[] { "Tests.Shared" }));
            }

            Assert.DoesNotThrow(() => File.Delete(native));
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [Test]
    public void A_dll_with_a_pdb_that_is_not_portable_keeps_its_references() {
        var bin = TempFolder.Create();
        try {
            File.Copy(typeof(Repo).Assembly.Location, Path.Combine(bin, "Tests.Shared.dll"));
            File.WriteAllText(Path.Combine(bin, "Tests.Shared.pdb"), "not a portable pdb");

            using var reader = new BinReader(bin);
            var dll = reader.Index().Dlls.Single();

            Assert.That(dll.References, Is.Not.Empty);
            Assert.That(dll.Documents, Is.Empty);
            Assert.That(dll.Types, Is.Empty);
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [Test]
    public void An_entry_file_has_the_path_case_of_the_disk() {
        if (!OperatingSystem.IsWindows()) {
            Assert.Ignore("Only Windows has paths that ignore the case.");
        }
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, EntryFiles.Name);
            File.WriteAllText(file, $"umpire.architecture = \"{VsaArchitecture}\"");

            Assert.That(EntryFiles.Read(file.ToUpperInvariant()), Is.EqualTo(EntryFiles.Read(file.ToLowerInvariant())));
        } finally {
            TempFolder.Delete(root);
        }
    }

    static string NativeRuntimeFile() {
        var name = OperatingSystem.IsWindows() ? "coreclr.dll" : OperatingSystem.IsMacOS() ? "libcoreclr.dylib" : "libcoreclr.so";
        return Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), name);
    }

    static void WriteFile(string root, string path, string content) {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }

    static BlockEntry Pack(string folder, string architecture, string assembly = "A") =>
        new(Path.GetFullPath(folder), EntryForm.Definition, TypeName.Parse(PackArchitecture),
            Path.Combine(Path.GetFullPath(folder), "Definition", "MyArchitecture.cs"), Packed: new TypeName(architecture, assembly));

    static BlockEntry Entry(string folder, EntryForm form) =>
        new(Path.GetFullPath(folder), form, form == EntryForm.App ? null : TypeName.Parse(VsaArchitecture),
            Path.Combine(Path.GetFullPath(folder), form.ToString()));

    static IReadOnlyList<EntryForm> EntriesIn(string folder) => [.. Entries.Where(e => Same(e.Folder, folder)).Select(e => e.Form).Distinct()];

    static bool Same(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)), StringComparison.OrdinalIgnoreCase);
}
