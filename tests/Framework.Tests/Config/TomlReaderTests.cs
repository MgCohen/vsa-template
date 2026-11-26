namespace Framework.Tests.Config;

using Umpire.Config;

public sealed class TomlReaderTests {
    [Test]
    public void A_comment_and_a_blank_line_give_no_value() =>
        Assert.That(Values("# a comment\n\n   \t\n# another\n"), Is.Empty);

    [Test]
    public void A_comment_after_a_value_is_not_part_of_the_value() =>
        Assert.That(Values("a = \"x # y\" # the comment\nb = 1#no space"),
            Is.EqualTo(new Dictionary<string, string> { ["a"] = "x # y", ["b"] = "1" }));

    [Test]
    public void A_bare_key_has_letters_digits_underscores_and_dashes() =>
        Assert.That(Values("Max_notes-2 = 3"), Is.EqualTo(new Dictionary<string, string> { ["Max_notes-2"] = "3" }));

    [Test]
    public void A_quoted_key_can_have_a_dot_and_a_slash() =>
        Assert.That(Values("[aliases]\n\"src/Game.Core\" = 1\n'lit.key' = 2"), Is.EqualTo(new Dictionary<string, string> {
            ["aliases.src/Game.Core"] = "1",
            ["aliases.lit.key"] = "2",
        }));

    [Test]
    public void A_dotted_key_makes_the_inner_tables() =>
        Assert.That(UmpireConfig.Parse("umpire . UMP010101 . max = 60").Value("umpire.UMP010101.max"), Is.EqualTo("60"));

    [Test]
    public void A_dotted_table_header_makes_the_inner_tables() =>
        Assert.That(UmpireConfig.Parse("[umpire.check]\nmode = \"Error\"").Value("umpire.check.mode"), Is.EqualTo("Error"));

    [Test]
    public void A_header_can_open_a_table_that_an_inner_header_made() =>
        Assert.That(Values("[a.b]\nx = 1\n[a]\ny = 2"), Is.EqualTo(new Dictionary<string, string> { ["a.b.x"] = "1", ["a.y"] = "2" }));

    [Test]
    public void A_basic_string_reads_its_escapes() =>
        Assert.That(UmpireConfig.Parse("a = \"q\\\" b\\\\ t\\t n\\n u\\u00e9 U\\U0001F600\"").Value("a"),
            Is.EqualTo("q\" b\\ t\t n\n u\u00e9 U\U0001F600"));

    [Test]
    public void A_literal_string_has_no_escapes() =>
        Assert.That(UmpireConfig.Parse(@"a = 'C:\Game\n'").Value("a"), Is.EqualTo(@"C:\Game\n"));

    [Test]
    public void An_empty_string_is_a_value() =>
        Assert.That(UmpireConfig.Parse("a = \"\"").Value("a"), Is.EqualTo(""));

    [TestCase("a = 0", "0")]
    [TestCase("a = 42", "42")]
    [TestCase("a = +7", "7")]
    [TestCase("a = -12", "-12")]
    [TestCase("a = 1_000", "1000")]
    [TestCase("a = 9223372036854775807", "9223372036854775807")]
    public void An_integer_gives_its_decimal_text(string text, string value) =>
        Assert.That(UmpireConfig.Parse(text).Value("a"), Is.EqualTo(value));

    [TestCase("a = true", "true")]
    [TestCase("a = false", "false")]
    public void A_boolean_gives_its_text(string text, string value) =>
        Assert.That(UmpireConfig.Parse(text).Value("a"), Is.EqualTo(value));

    [Test]
    public void An_array_of_strings_gives_a_list() =>
        Assert.That(Lists("[t]\na = [\"x\", 'y', \"\"]")["a"], Is.EqualTo(new[] { "x", "y", "" }));

    [Test]
    public void An_array_of_numbers_and_booleans_gives_their_text() =>
        Assert.That(Lists("[t]\na = [1, -2, true]")["a"], Is.EqualTo(new[] { "1", "-2", "true" }));

    [Test]
    public void An_array_can_have_many_lines_comments_and_a_last_comma() =>
        Assert.That(Lists("[t]\na = [\n  \"x\", # first\n\n  \"y\",\n]\nb = []")["a"], Is.EqualTo(new[] { "x", "y" }));

    [Test]
    public void An_empty_array_gives_an_empty_list() =>
        Assert.That(Lists("[t]\nb = [ ]")["b"], Is.Empty);

    [Test]
    public void An_inline_table_gives_a_table() =>
        Assert.That(Values("[t]\nModes = { easy = 1, \"hard\" = 5, deep.x = 'y', none = {} }"), Is.EqualTo(new Dictionary<string, string> {
            ["t.Modes.easy"] = "1",
            ["t.Modes.hard"] = "5",
            ["t.Modes.deep.x"] = "y",
        }));

    [Test]
    public void Windows_line_ends_and_a_byte_order_mark_are_read() =>
        Assert.That(Values("\uFEFFa = 1\r\n[t]\r\nb = \"x\"\r\n"),
            Is.EqualTo(new Dictionary<string, string> { ["a"] = "1", ["t.b"] = "x" }));

    [Test]
    public void The_keys_keep_the_order_of_the_file() =>
        Assert.That(UmpireConfig.Parse("z = 1\na = 2\nm = 3").Values().Select(pair => pair.Key), Is.EqualTo(new[] { "z", "a", "m" }));

    [TestCase("a = 1\n[*.cs]\nb = 2\n", 2, 2, "A key cannot start with '*'")]
    [TestCase("a = 1\na = 2", 2, 1, "The key a is defined two times")]
    [TestCase("[t]\n[t]", 2, 1, "The table [t] is defined two times")]
    [TestCase("[t]\nb.c = 1\n[t.b]", 3, 1, "The table [t.b] is defined two times")]
    [TestCase("a = 1\n[a]", 2, 1, "The key a has a value, so it cannot also be a table")]
    [TestCase("a = 1\na.b = 2", 2, 1, "The key a.b cannot add a value to a")]
    [TestCase("[a.b]\n[a]\nb = 1", 3, 1, "The key b is defined two times")]
    [TestCase("a", 1, 2, "Write '=' after the key a")]
    [TestCase("a =", 1, 4, "A value is missing")]
    [TestCase("a = # nothing", 1, 5, "A value is missing")]
    [TestCase("\n\na = Error", 3, 5, "The value Error is not valid. A string needs quotes")]
    [TestCase("a = \"open", 1, 10, "The string has no closing quote")]
    [TestCase("a = 'open", 1, 10, "The string has no closing quote")]
    [TestCase("a = \"bad \\q\"", 1, 10, "The escape \\q is not valid")]
    [TestCase("a = \"\\u12\"", 1, 6, "The escape needs 4 hex digits")]
    [TestCase("a = \"\\uD800\"", 1, 6, "The escape needs 4 hex digits")]
    [TestCase("a = \"\"\"x\"\"\"", 1, 5, "A multi-line string (\"\"\") is not supported")]
    [TestCase("a = '''x'''", 1, 5, "A multi-line string (''') is not supported")]
    [TestCase("a = { b = 1", 1, 12, "The inline table has no closing brace")]
    [TestCase("a = { b = 1 c = 2 }", 1, 13, "Put a comma (,) between the keys of an inline table")]
    [TestCase("a = { b = 1 }\n[a]", 2, 1, "The table [a] is defined two times")]
    [TestCase("a = { b = 1 }\na.c = 2", 2, 1, "The key a.c cannot add a value to a")]
    [TestCase("a = { b = 1 }\n[a.c]", 2, 1, "The table [a.c] cannot add a key to the inline table a")]
    public void A_key_or_a_table_that_the_reader_does_not_read_names_its_line_and_column(
        string text, int line, int column, string reason) =>
        AssertError(text, line, column, reason);

    [TestCase("[[a]]", 1, 1, "An array of tables ([[name]]) is not supported")]
    [TestCase("a = [[1]]", 1, 6, "An array in an array is not supported")]
    [TestCase("a = [1 2]", 1, 8, "Put a comma (,) between the items of an array")]
    [TestCase("a = [1,\n2", 2, 2, "The array has no closing bracket")]
    [TestCase("a = 1.5", 1, 5, "The float 1.5 is not supported")]
    [TestCase("a = inf", 1, 5, "The float inf is not supported")]
    [TestCase("a = 0x1F", 1, 5, "The integer 0x1F is not supported")]
    [TestCase("a = 007", 1, 5, "The value 007 is not valid")]
    [TestCase("a = 1__0", 1, 5, "The value 1__0 is not valid")]
    [TestCase("a = 1979-05-27", 1, 5, "The date or time 1979-05-27 is not supported")]
    [TestCase("a = 07:32:00", 1, 5, "The date or time 07:32:00 is not supported")]
    [TestCase("a = 9223372036854775808", 1, 5, "The integer 9223372036854775808 is out of range")]
    [TestCase("a = 1 b = 2", 1, 7, "The line has more text after its end")]
    [TestCase("[t] x", 1, 5, "The line has more text after its end")]
    [TestCase("[t", 1, 3, "Write ']' at the end of the table header")]
    [TestCase("a = \"tab\u0001\"", 1, 9, "A string cannot have a control character")]
    public void A_value_that_the_reader_does_not_read_names_its_line_and_column(
        string text, int line, int column, string reason) =>
        AssertError(text, line, column, reason);

    [Test]
    public void Get_binds_the_types_of_a_settings_class() {
        var settings = UmpireConfig.Parse("""
            [Game]
            Count = 3
            Big = 9000000000
            Ratio = 2
            On = true
            Level = "hard"
            Maybe = 4
            Names = ["a", "b"]
            Ids = [1, 2]
            [Game.Scores]
            x = 1
            """).Get<GameSettings>("Game")!;

        var values = new object?[] { settings.Count, settings.Big, settings.Ratio, settings.On, settings.Level, settings.Maybe };
        Assert.That(values,
            Is.EqualTo(new object?[] { 3, 9000000000L, 2.0, true, GameLevel.Hard, 4 }));
        Assert.That(settings.Names, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(settings.Ids, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(settings.Scores, Is.EqualTo(new Dictionary<string, int> { ["x"] = 1 }));
    }

    [Test]
    public void Get_binds_a_struct() =>
        Assert.That(UmpireConfig.Parse("[Game]\nRange = { Min = 2, Max = 7 }").Get<GameSettings>("Game")!.Range,
            Is.EqualTo(new GameRange { Min = 2, Max = 7 }));

    [Test]
    public void Get_skips_a_key_with_no_property() =>
        Assert.That(UmpireConfig.Parse("[Game]\nUnknown = 1\nCount = 2").Get<GameSettings>("Game")!.Count,
            Is.EqualTo(2));

    [TestCase("Count = \"3\"", "Game.Count needs Int32, and the value is the string \"3\".")]
    [TestCase("Count = 9000000000", "Game.Count needs Int32, and the value 9000000000 is out of its range.")]
    [TestCase("On = 1", "Game.On needs Boolean, and the value is the integer 1.")]
    [TestCase("Level = \"medium\"", "Game.Level needs one of Easy, Hard, and the value is \"medium\".")]
    [TestCase("Names = \"a\"", "Game.Names needs a list of String, and the value is the string \"a\".")]
    [TestCase("Names = { a = \"x\" }", "Game.Names needs a list of String, and the value is a table.")]
    [TestCase("Ids = [1, \"2\"]", "Game.Ids[1] needs Int32, and the value is the string \"2\".")]
    public void Get_with_a_value_of_the_wrong_type_names_the_key(string line, string reason) {
        var error = Assert.Throws<InvalidOperationException>(() => UmpireConfig.Parse("[Game]\n" + line).Get<GameSettings>("Game"))!;

        Assert.That(error.Message, Is.EqualTo($"The table [Game] of the TOML text does not fit GameSettings: {reason}"));
    }

    static void AssertError(string text, int line, int column, string reason) {
        var error = Assert.Throws<ConfigFormatException>(() => UmpireConfig.Parse(text))!;

        Assert.That(new[] { error.Line, error.Column }, Is.EqualTo(new[] { line, column }), error.Message);
        Assert.That(error.Reason, Does.StartWith(reason));
        Assert.That(error.Message, Does.StartWith($"({line},{column}): "));
    }

    static Dictionary<string, string> Values(string text) =>
        UmpireConfig.Parse(text).Values().ToDictionary(pair => pair.Key, pair => pair.Value);

    static Dictionary<string, IReadOnlyList<string>?> Lists(string text) =>
        UmpireConfig.Parse(text).Lists("t").ToDictionary(pair => pair.Key, pair => pair.Value);
}

public enum GameLevel {
    Easy,
    Hard,
}

public sealed class GameSettings {
    public int Count { get; set; }

    public long Big { get; set; }

    public double Ratio { get; set; }

    public bool On { get; set; }

    public GameLevel Level { get; set; }

    public int? Maybe { get; set; }

    public List<string> Names { get; set; } = [];

    public int[] Ids { get; set; } = [];

    public IReadOnlyDictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();

    public GameRange Range { get; set; }
}

public struct GameRange {
    public int Min { get; set; }

    public int Max { get; set; }
}
