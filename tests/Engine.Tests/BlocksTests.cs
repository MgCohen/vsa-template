using Blocks.Infrastructure;
using Blocks.Slices.FindBlocks;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Tests.Shared;
using UmpireEngine;

namespace Engine.Tests;

public sealed class BlocksTests {
    const string PackArchitecture = "Umpire.Kernel.PackArchitecture, Umpire.Kernel";
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
    public void An_app_class_is_the_check_entry_of_the_folder_of_its_project() {
        Assert.That(EntriesIn(Fixture), Is.EqualTo(new[] { EntryForm.App }));
    }

    [TestCase("Alpha")]
    [TestCase("Beta")]
    public void A_concrete_pack_class_is_the_check_entry_of_its_own_folder(string pack) {
        var entry = Entries.Single(e => Same(e.Folder, Path.Combine(Fixture, "Packs", pack)));

        Assert.That(entry.Form, Is.EqualTo(EntryForm.Base));
        Assert.That(entry.Type, Is.EqualTo(TypeName.Parse(PackArchitecture)));
    }

    [Test]
    public void An_app_with_its_own_pack_has_the_app_entry_and_the_pack_entry() {
        var app = Repo.Path("tests", "Fixtures", "PackVariant");

        Assert.That(EntriesIn(app), Is.EqualTo(new[] { EntryForm.App }));
        Assert.That(EntriesIn(Path.Combine(app, "Packs", "Ecs")), Is.EqualTo(new[] { EntryForm.Base }));
    }

    [Test]
    public void Assembly_metadata_with_Umpire_Root_is_the_check_entry_of_the_folder_above_the_project() {
        var entry = Entries.Single(e => Same(e.Folder, Path.Combine(Fixture, "Lib")));

        Assert.That(entry.Form, Is.EqualTo(EntryForm.Metadata));
        Assert.That(entry.Type, Is.EqualTo(TypeName.Parse(VsaArchitecture)));
        Assert.That(entry.Source, Does.EndWith("Lib.csproj"));
    }

    [Test]
    public void An_abstract_pack_base_is_no_check_entry() {
        Assert.That(EntriesIn(Repo.Path("framework", "Kernel", "App")), Is.Empty);
    }

    [Test]
    public void Two_pack_classes_with_one_architecture_in_one_folder_are_one_check_entry() {
        var vsa = Entries.Where(e => Same(e.Folder, Repo.Path("packs", "Vsa"))).ToList();

        Assert.That(vsa.Select(e => e.Key).Distinct(), Is.EqualTo(new[] { $"Base: {PackArchitecture}" }));
    }

    [TestCase("Lib.Missing, Umpire.Vsa", "Umpire.Vsa has no type Lib.Missing")]
    [TestCase("Umpire.Kernel.Pack, Umpire.Kernel", "does not inherit Umpire.Architecture.Architecture")]
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
        Assert.That(app.Bases, Does.Contain("Umpire.Kernel.App"));
        Assert.That(app.HasEmptyConstructor, Is.True);
    }

    [Test]
    public void The_leaves_of_the_app_classes_drop_each_base_app_class() {
        var first = new AppClass(new TypeName("A.First", "A"), "A.cs", true, ["Umpire.Kernel.App"]);
        var second = new AppClass(new TypeName("A.Second", "A"), "B.cs", true, ["A.First", "Umpire.Kernel.App"]);

        Assert.That(AppClass.Leaves([first, second]), Is.EqualTo(new[] { second }));
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

        Assert.That(app.Select(d => d.Name), Is.EqualTo(new[] { "Blocks" }));
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
    public void No_block_gives_UMP030103() {
        var result = BlockSearch.Find("C:/z", null, [], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030103"));
    }

    [Test]
    public void Two_entries_in_the_block_give_UMP030101_and_stop_the_run() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), Entry("C:/a", EntryForm.File)], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030101"));
        Assert.That(result.Error.Message, Does.Contain("more than one check entry"));
    }

    [Test]
    public void Two_entries_in_an_inner_block_give_UMP030101_and_the_run_continues() {
        var inner = new[] { Entry("C:/a/t", EntryForm.Base), Entry("C:/a/t", EntryForm.File) };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), .. inner], NoBin);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "UMP030101" }));
        Assert.That(result.Value.Blocks.InnerFolders, Is.EqualTo(new[] { Path.GetFullPath("C:/a/t") }));
    }

    [Test]
    public void Two_entry_forms_in_one_file_give_UMP030101() {
        var app = Entry("C:/a", EntryForm.App);
        var typed = app with { Form = EntryForm.Base, Type = TypeName.Parse(VsaArchitecture) };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [app, typed], NoBin);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030101"));
    }

    [Test]
    public void An_architecture_with_no_empty_constructor_gives_its_reason() {
        using var reader = new BinReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem(new TypeName(typeof(NamedArchitecture).FullName!, "Engine.Tests")),
            Does.Contain("has no public constructor with no parameters"));
    }

    [Test]
    public void A_bad_name_in_an_inner_block_gives_UMP030102() {
        var bad = Entry("C:/a/t", EntryForm.File).Names("X", TypeName.NoAssembly);

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), bad], NoBin);

        Assert.That(result.Value.Findings.Single().Code, Is.EqualTo("UMP030102"));
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
                File.WriteAllText(Path.Combine(root, folder, EntryFiles.Name), $$"""{ "architecture": "{{VsaArchitecture}}" }""");
            }

            var found = EntryFiles.Below(root);

            Assert.That(found.Select(f => f.Folder), Is.EqualTo(new[] { Path.Combine(Path.GetFullPath(root), "Tools") }));
            Assert.That(found.Single().Type, Is.EqualTo(TypeName.Parse(VsaArchitecture)));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase("not json", "does not name an architecture: it is not valid JSON")]
    [TestCase("""{ "name": "x" }""", "does not name an architecture: it has no string property \"architecture\"")]
    [TestCase("""{ "architecture": "Lib.X" }""", "names \"Lib.X\", but the name has no assembly")]
    public void A_bad_entry_file_gives_its_reason(string text, string reason) {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, EntryFiles.Name);
            File.WriteAllText(file, text);

            Assert.That(EntryFiles.Read(file).Problem, Does.StartWith(reason));
        } finally {
            TempFolder.Delete(root);
        }
    }

    static BlockEntry Entry(string folder, EntryForm form) =>
        new(Path.GetFullPath(folder), form, form == EntryForm.App ? null : TypeName.Parse(VsaArchitecture),
            Path.Combine(Path.GetFullPath(folder), form.ToString()));

    static IReadOnlyList<EntryForm> EntriesIn(string folder) => [.. Entries.Where(e => Same(e.Folder, folder)).Select(e => e.Form).Distinct()];

    static bool Same(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)), StringComparison.OrdinalIgnoreCase);
}
