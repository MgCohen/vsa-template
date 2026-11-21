using Tests.Shared;

namespace Framework.Tests.Config;

using Tomlyn;
using Umpire.Config;

public sealed class UmpireConfigTests {
    const string Text = """
        umpire.architecture = "Notes.App.Host.NotesArchitecture"
        umpire.UMP010101.max = 60
        dotnet_diagnostic.UMP010106.severity = "none"

        [Notebook]
        MaxNotes = 50
        Limits.MaxLines = 80
        Tags = ["work", "home"]
        Modes = { easy = 1, hard = 5 }
        """;

    static readonly UmpireConfig Config = UmpireConfig.Parse(Text);

    [Test]
    public void Lists_gives_each_array_of_a_table_and_skips_the_other_values() {
        var lists = Config.Lists("Notebook").ToList();

        Assert.That(lists.Select(pair => pair.Key), Is.EqualTo(new[] { "Tags" }));
        Assert.That(lists[0].Value, Is.EqualTo(new[] { "work", "home" }));
    }

    [Test]
    public void Lists_of_a_missing_table_is_empty() => Assert.That(Config.Lists("Missing"), Is.Empty);

    [Test]
    public void Get_gives_the_values_of_a_table() =>
        Assert.That(Config.Get<NotebookSettings>("Notebook")!.MaxNotes, Is.EqualTo(50));

    [Test]
    public void A_missing_key_keeps_its_default() =>
        Assert.That(Config.Get<NotebookSettings>("Notebook")!.Title, Is.EqualTo("Notes"));

    [Test]
    public void A_nested_object_gets_its_values_and_keeps_its_defaults() {
        var limits = Config.Get<NotebookSettings>("Notebook")!.Limits;

        Assert.That(limits.MaxLines, Is.EqualTo(80));
        Assert.That(limits.MinLines, Is.EqualTo(1));
    }

    [Test]
    public void An_array_gives_a_list() =>
        Assert.That(Config.Get<NotebookSettings>("Notebook")!.Tags, Is.EqualTo(new[] { "work", "home" }));

    [Test]
    public void An_inline_table_gives_a_dictionary() =>
        Assert.That(Config.Get<NotebookSettings>("Notebook")!.Modes,
            Is.EqualTo(new Dictionary<string, int> { ["easy"] = 1, ["hard"] = 5 }));

    [Test]
    public void A_missing_table_gives_null() =>
        Assert.That(Config.Get<NotebookSettings>("Shelf"), Is.Null);

    [Test]
    public void A_property_name_matches_with_no_case() =>
        Assert.That(UmpireConfig.Parse("[Notebook]\nmaxnotes = 3").Get<NotebookSettings>("Notebook")!.MaxNotes,
            Is.EqualTo(3));

    [Test]
    public void A_value_of_the_wrong_type_names_the_table_and_the_file() {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, UmpireConfig.FileName);
            File.WriteAllText(file, "[Notebook]\nMaxNotes = \"50\"\n");

            var error = Assert.Throws<InvalidOperationException>(() => UmpireConfig.In(root).Get<NotebookSettings>("Notebook"));

            Assert.That(error!.Message, Does.StartWith($"The table [Notebook] of {Path.GetFullPath(file)} does not fit NotebookSettings: "));
            Assert.That(error.InnerException, Is.TypeOf<TomlException>());
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void In_reads_only_the_file_of_the_folder() {
        var root = TempFolder.Create();
        try {
            var inner = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
            File.WriteAllText(Path.Combine(root, UmpireConfig.FileName), "umpire.max = 3");

            Assert.That(UmpireConfig.In(inner).FilePath, Is.Null);
            Assert.That(UmpireConfig.In(root).Value("umpire.max"), Is.EqualTo("3"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void In_with_no_folder_gives_Empty(string? folder) =>
        Assert.That(UmpireConfig.In(folder), Is.SameAs(UmpireConfig.Empty));

    [Test]
    public void Values_of_a_table_gives_only_the_leaves_of_that_table() =>
        Assert.That(Config.Values("umpire").Select(pair => pair.Key),
            Is.EqualTo(new[] { "umpire.architecture", "umpire.UMP010101.max" }));

    [Test]
    public void Get_uses_a_serializer_context() {
        var settings = Config.Get<NotebookSettings>("Notebook", NotebookContext.Default);

        Assert.That(settings!.MaxNotes, Is.EqualTo(50));
    }

    [Test]
    public void Value_walks_a_dotted_path_with_no_case() {
        Assert.That(Config.Value("UMPIRE.ump010101.MAX"), Is.EqualTo("60"));
        Assert.That(Config.Value("umpire.architecture"), Is.EqualTo("Notes.App.Host.NotesArchitecture"));
        Assert.That(Config.Value("umpire.missing"), Is.Null);
        Assert.That(Config.Value("Notebook"), Is.Null);
    }

    [Test]
    public void Values_gives_each_scalar_as_a_dotted_key() =>
        Assert.That(Config.Values().ToDictionary(pair => pair.Key, pair => pair.Value), Is.EqualTo(new Dictionary<string, string> {
            ["umpire.architecture"] = "Notes.App.Host.NotesArchitecture",
            ["umpire.UMP010101.max"] = "60",
            ["dotnet_diagnostic.UMP010106.severity"] = "none",
            ["Notebook.MaxNotes"] = "50",
            ["Notebook.Limits.MaxLines"] = "80",
            ["Notebook.Modes.easy"] = "1",
            ["Notebook.Modes.hard"] = "5",
        }));

    [Test]
    public void A_file_pattern_section_is_not_valid_TOML() {
        var error = Assert.Throws<TomlException>(() => UmpireConfig.Parse("a = 1\n[*.cs]\nb = 2\n"));

        Assert.That(error!.Line, Is.EqualTo(2));
    }

    [Test]
    public void Find_reads_the_nearest_file_at_or_above_the_folder() {
        var root = TempFolder.Create();
        try {
            var inner = Directory.CreateDirectory(Path.Combine(root, "App", "Host")).FullName;
            File.WriteAllText(Path.Combine(root, UmpireConfig.FileName), "umpire.max = 3");

            var config = UmpireConfig.Find(inner);

            Assert.That(config.Value("umpire.max"), Is.EqualTo("3"));
            Assert.That(config.FilePath, Is.EqualTo(Path.Combine(Path.GetFullPath(root), UmpireConfig.FileName)));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void A_parse_error_of_a_file_names_the_file_and_the_line() {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, UmpireConfig.FileName);
            File.WriteAllText(file, "umpire.max = 3\n[*.cs]\n");

            var error = Assert.Throws<TomlException>(() => UmpireConfig.Find(root));

            Assert.That(error!.Message, Does.StartWith(Path.GetFullPath(file) + "(2,"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void Empty_has_no_values() {
        Assert.That(UmpireConfig.Empty.Values(), Is.Empty);
        Assert.That(UmpireConfig.Empty.Get<NotebookSettings>("Notebook"), Is.Null);
        Assert.That(UmpireConfig.Empty.FilePath, Is.Null);
    }
}

public sealed class NotebookSettings {
    public int MaxNotes { get; set; } = 10;

    public string Title { get; set; } = "Notes";

    public NotebookLimits Limits { get; set; } = new();

    public List<string> Tags { get; set; } = [];

    public Dictionary<string, int> Modes { get; set; } = [];
}

public sealed class NotebookLimits {
    public int MaxLines { get; set; } = 40;

    public int MinLines { get; set; } = 1;
}
