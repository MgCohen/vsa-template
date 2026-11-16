using Blocks.Infrastructure;
using Blocks.Slices.FindBlocks;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

public sealed class BlocksTests {
    const string PackArchitecture = "Umpire.Kernel.PackArchitecture, Umpire.Kernel";
    const string VsaArchitecture = "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa";
    static readonly string Fixture = Repo.Path("tests", "Fixtures", "Blocks");
    static IReadOnlyList<BlockEntry>? entries;

    static IReadOnlyList<BlockEntry> Entries {
        get {
            if (entries is null) {
                using var reader = new EntryReader(AppContext.BaseDirectory);
                entries = reader.Entries();
            }
            return entries;
        }
    }

    [Test]
    public void An_app_class_is_the_check_entry_of_the_folder_of_its_project() {
        Assert.That(EntriesIn(Fixture), Is.EqualTo(new[] { EntryForm.App }));
    }

    [TestCase("Alpha")]
    [TestCase("Beta")]
    public void A_concrete_pack_class_is_the_check_entry_of_its_own_folder(string pack) {
        var entry = Entries.Single(e => Same(e.Folder, Path.Combine(Fixture, "Packs", pack)));

        Assert.That(entry.Form, Is.EqualTo(EntryForm.Base));
        Assert.That(entry.Architecture, Is.EqualTo(PackArchitecture));
    }

    [Test]
    public void Assembly_metadata_with_Umpire_Root_is_the_check_entry_of_the_folder_above_the_project() {
        var entry = Entries.Single(e => Same(e.Folder, Path.Combine(Fixture, "Lib")));

        Assert.That(entry.Form, Is.EqualTo(EntryForm.Metadata));
        Assert.That(entry.Architecture, Is.EqualTo(VsaArchitecture));
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
    [TestCase("Umpire.Vsa.Definition.VsaArchitecture", "the name has no assembly")]
    [TestCase("Some.Architecture, Some.Missing", "the build output has no assembly Some.Missing")]
    public void A_bad_architecture_name_gives_its_reason(string name, string reason) {
        using var reader = new EntryReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem(name), Does.Contain(reason));
    }

    [Test]
    public void A_good_architecture_name_gives_no_problem() {
        using var reader = new EntryReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem(VsaArchitecture), Is.Null);
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
        var result = BlockSearch.Find("C:/z", null, [], _ => null);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030103"));
    }

    [Test]
    public void Two_entries_in_the_block_give_UMP030101_and_stop_the_run() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), Entry("C:/a", EntryForm.File)], _ => null);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030101"));
        Assert.That(result.Error.Message, Does.Contain("more than one check entry"));
    }

    [Test]
    public void Two_entries_in_an_inner_block_give_UMP030101_and_the_run_continues() {
        var inner = new[] { Entry("C:/a/t", EntryForm.Base), Entry("C:/a/t", EntryForm.File) };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), .. inner], _ => null);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "UMP030101" }));
        Assert.That(result.Value.Blocks.InnerFolders, Is.EqualTo(new[] { Path.GetFullPath("C:/a/t") }));
    }

    [Test]
    public void Two_entry_forms_in_one_file_give_UMP030101() {
        var app = Entry("C:/a", EntryForm.App);
        var typed = app with { Form = EntryForm.Base, Architecture = VsaArchitecture };

        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [app, typed], _ => null);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030101"));
    }

    [Test]
    public void An_architecture_with_no_empty_constructor_gives_its_reason() {
        using var reader = new EntryReader(AppContext.BaseDirectory);

        Assert.That(reader.Problem($"{typeof(NamedArchitecture).FullName}, Engine.Tests"),
            Does.Contain("has no public constructor with no parameters"));
    }

    [Test]
    public void A_bad_name_in_an_inner_block_gives_UMP030102() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [Entry("C:/a", EntryForm.App), Entry("C:/a/t", EntryForm.File)],
            entry => entry.Form == EntryForm.File ? "names \"X\", but the name has no assembly" : null);

        Assert.That(result.Value.Findings.Single().Code, Is.EqualTo("UMP030102"));
        Assert.That(result.Value.Findings.Single().Message, Does.Contain("for example \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\""));
    }

    [Test]
    public void A_declared_folder_with_no_entry_is_an_app_block() {
        var result = BlockSearch.Find("C:/a", Path.GetFullPath("C:/a"), [], _ => null);

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

            Assert.That(found.Select(f => f.Entry.Folder), Is.EqualTo(new[] { Path.Combine(Path.GetFullPath(root), "Tools") }));
            Assert.That(found.Single().Entry.Architecture, Is.EqualTo(VsaArchitecture));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase("not json", "it is not valid JSON")]
    [TestCase("""{ "name": "x" }""", "it has no string property \"architecture\"")]
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
        new(Path.GetFullPath(folder), form, form == EntryForm.App ? null : VsaArchitecture, Path.Combine(Path.GetFullPath(folder), form.ToString()));

    static IReadOnlyList<EntryForm> EntriesIn(string folder) => [.. Entries.Where(e => Same(e.Folder, folder)).Select(e => e.Form).Distinct()];

    static bool Same(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)), StringComparison.OrdinalIgnoreCase);
}
