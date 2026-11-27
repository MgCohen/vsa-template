using Tests.Shared;
using CodeCheck.Slices.CheckCode;
using Facts.Domain.Shared.Compile;

namespace Rules.Tests;

public sealed class LineLengthRuleTests {
    static readonly CodeRule Rule = new LineLengthRule();

    [Test]
    public Task Line_at_the_limit_passes() => Analyzer.VerifyFormatting(Rule, """
        class Abc { int v; }
        """, "umpire.UMP010101.max = 20");

    [Test]
    public Task Line_over_the_limit_fails() => Analyzer.VerifyFormatting(Rule, """
        class Abc { int valu{|#0:e; }|}
        """, "umpire.UMP010101.max = 20", Analyzer.Diagnostic(Rule, 0, 24, 20));

    [Test]
    public Task Limit_comes_from_the_default_of_the_rule() {
        var max = int.Parse(TestConfig.Default(new LineLengthRule(), LimitRule.MaxKey));
        var comment = "// " + new string('x', max - 3);
        var source = string.Join("\n", "class A { }", comment, comment + "{|#0:x|}");
        return Analyzer.VerifyFormatting(Rule, source, "", Analyzer.Diagnostic(Rule, 0, max + 1, max));
    }
}

public sealed class MethodLengthRuleTests {
    static readonly CodeRule Rule = new MethodLengthRule();

    const string Method = """
        class A {
            int {|#0:M|}() {
                var a = 1;
                return a;
            }
        }
        """;

    [Test]
    public Task Method_at_the_limit_passes() =>
        Analyzer.VerifyFormatting(Rule, Method.Replace("{|#0:M|}", "M"), "umpire.UMP010102.max = 4");

    [Test]
    public Task Method_over_the_limit_fails() =>
        Analyzer.VerifyFormatting(Rule, Method, "umpire.UMP010102.max = 3", Analyzer.Diagnostic(Rule, 0, "M", 4, 3));

    [Test]
    public Task Constructor_over_the_limit_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
            int a;
            {|#0:A|}() {
                a = 1;
            }
        }
        """, "umpire.UMP010102.max = 2", Analyzer.Diagnostic(Rule, 0, "Constructor A", 3, 2));

    [Test]
    public Task Global_max_option_applies() =>
        Analyzer.VerifyFormatting(Rule, Method, "umpire.max = 3", Analyzer.Diagnostic(Rule, 0, "M", 4, 3));
}

public sealed class FileLengthRuleTests {
    static readonly CodeRule Rule = new FileLengthRule();

    [Test]
    public Task File_at_the_limit_passes() => Analyzer.VerifyFormatting(Rule, """
        class A {
        }
        // end
        """, "umpire.UMP010103.max = 3");

    [Test]
    public Task File_over_the_limit_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
        }
        // one
        {|#0:// two|}
        """, "umpire.UMP010103.max = 3", Analyzer.Diagnostic(Rule, 0, 4, 3));
}

public sealed class OneTypePerFileRuleTests {
    static readonly CodeRule Rule = new OneTypePerFileRule();

    [Test]
    public Task One_type_with_nested_types_passes() => Analyzer.VerifyFormatting(Rule, """
        class A {
            class B { }
            enum C { One }
        }
        """, "");

    [Test]
    public Task Second_type_fails() => Analyzer.VerifyFormatting(Rule, """
        namespace N {
            class A { }
            record {|#0:B|};
        }
        """, "", Analyzer.Diagnostic(Rule, 0, "B", "A"));

    [Test]
    public Task Delegate_is_a_type() => Analyzer.VerifyFormatting(Rule, """
        class A { }
        delegate void {|#0:Done|}();
        """, "", Analyzer.Diagnostic(Rule, 0, "Done", "A"));

    [Test]
    public Task Types_with_the_same_name_pass() => Analyzer.VerifyFormatting(Rule, """
        abstract class Module { }
        abstract class Module<TSettings> : Module { }
        """, "");

    [Test]
    public Task Option_turns_off_the_same_name_exception() => Analyzer.VerifyFormatting(Rule, """
        abstract class Module { }
        abstract class {|#0:Module|}<TSettings> : Module { }
        """, "umpire.UMP010104.allow_same_name = false", Analyzer.Diagnostic(Rule, 0, "Module", "Module"));
}

public sealed class MemberOrderRuleTests {
    static readonly CodeRule Rule = new MemberOrderRule();

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Diagnostic(string name, string group, string previous,
        string order = "static before instance") =>
        Analyzer.Diagnostic(Rule, 0, name, group, previous, TestConfig.Default(new MemberOrderRule(), MemberOrderRule.KindOrderKey),
            TestConfig.Default(new MemberOrderRule(), MemberOrderRule.AccessOrderKey), order);

    [Test]
    public Task Members_in_order_pass() => Analyzer.VerifyFormatting(Rule, """
        class A : System.IDisposable {
            public const int Max = 1;
            public static readonly A Empty = new();
            int count;
            public A() { }
            static A() { }
            public int Count => count;
            public event System.Action? Changed;
            public void Add() => count++;
            void System.IDisposable.Dispose() { }
            protected void Raise() => Changed?.Invoke();
            static void Log() { }
            class Nested { }
        }
        """, "");

    [Test]
    public Task Field_after_method_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
            void M() { }
            int {|#0:value|};
        }
        """, "", Diagnostic("value", "private field", "private method"));

    [Test]
    public Task Public_after_private_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
            void M() { }
            public void {|#0:N|}() { }
        }
        """, "", Diagnostic("N", "public method", "private method"));

    [Test]
    public Task Static_after_instance_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
            void M() { }
            static void {|#0:N|}() { }
        }
        """, "", Diagnostic("N", "private static method", "private method"));

    [Test]
    public Task Interface_members_are_public() => Analyzer.VerifyFormatting(Rule, """
        interface I {
            int Count { get; }
            void M();
        }
        """, "");

    [Test]
    public Task Kind_order_option_changes_the_order() => Analyzer.VerifyFormatting(Rule, """
        class A {
            void M() { }
            int value;
        }
        """, "umpire.UMP010105.kind_order = method, field");

    [Test]
    public Task Static_first_option_changes_the_order() => Analyzer.VerifyFormatting(Rule, """
        class A {
            static void M() { }
            void {|#0:N|}() { }
        }
        """, "umpire.UMP010105.static_first = false",
        Diagnostic("N", "private method", "private static method", "instance before static"));
}

public sealed class CommentRuleTests {
    static readonly CodeRule Rule = new CommentRule();

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Comment(int location) => Analyzer.Diagnostic(Rule, location);

    [Test]
    public Task Directives_are_not_comments() => Analyzer.VerifyFormatting(Rule, """
        #pragma warning disable CS0169
        class A {
            #region Fields
            int a;
            #endregion
        }
        """, "");

    [Test]
    public Task Line_comment_fails() => Analyzer.VerifyFormatting(Rule, """
        {|#0:// text|}
        class A { }
        """, "", Comment(0));

    [Test]
    public Task Each_line_comment_fails() => Analyzer.VerifyFormatting(Rule, """
        {|#0:// one|}
        {|#1:// two|}
        class A { }
        """, "", Comment(0), Comment(1));

    [Test]
    public Task Comment_at_the_end_of_a_line_fails() => Analyzer.VerifyFormatting(Rule, """
        class A { } {|#0:// text|}
        """, "", Comment(0));

    [Test]
    public Task Block_comment_fails() => Analyzer.VerifyFormatting(Rule, """
        class A { {|#0:/* text */|} }
        """, "", Comment(0));

    [Test]
    public Task Xml_comment_fails_and_stops_at_the_end_of_the_line() => Analyzer.VerifyFormatting(Rule, """
        {|#0:/// <summary>Text.</summary>|}
        class A { }
        """, "", Comment(0));

    [Test]
    public Task Block_xml_comment_fails() => Analyzer.VerifyFormatting(Rule, """
        {|#0:/** <summary>Text.</summary> */|}
        class A { }
        """, "", Comment(0));

    [Test]
    public Task Comment_after_a_directive_fails() => Analyzer.VerifyFormatting(Rule, """
        #pragma warning disable CS0169 {|#0:// reason|}
        #if DEBUG {|#1:// why|}
        class A { int a; }
        #endif {|#2:// end|}
        """, "", Comment(0), Comment(1), Comment(2));

    [Test]
    public Task Comment_text_in_strings_passes() => Analyzer.VerifyFormatting(Rule, """"
        class A {
            string a = "// text";
            string b = @"/* text */";
            string c = """// text""";
        }
        """", "");

    [Test]
    public Task Inactive_code_is_not_checked() => Analyzer.VerifyFormatting(Rule, """
        #if NOT_DEFINED
        class B { } // text
        #endif
        class A { }
        """, "");
}

public sealed class RuleOptionTests {
    static readonly CodeRule Rule = new LineLengthRule();

    const string LongLine = "class Abc { int value; }";

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Option(string rule, string key, string kind, string value) =>
        new Microsoft.CodeAnalysis.Testing.DiagnosticResult(CodeRule.OptionNotice.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithMessage(CodeRule.OptionNotice.Text(rule, key, kind, value));

    [Test]
    public Task Missing_option_takes_the_default_of_the_rule() =>
        Analyzer.VerifyFormatting(Rule, "// " + new string('y', 117) + "{|#0:x|}", "",
            Analyzer.Diagnostic(Rule, 0, 121, 120));

    [Test]
    public void Option_with_no_default_and_no_value_fails_and_the_rule_does_not_run() {
        var run = NoDefaultRule.Run("");

        Assert.That(run.Notices.Select(n => n.Message), Is.EqualTo(new[] {
            CodeRule.OptionNotice.Text("UMP010199", NoDefaultRule.Key, "a whole number more than 0", "missing") }));
        Assert.That(run.Check(NoDefaultRule.File), Is.Empty);
    }

    [Test]
    public void Option_with_no_default_runs_with_a_value_from_the_config() {
        var run = NoDefaultRule.Run("umpire.UMP010199.size = 2");

        Assert.That(run.Notices, Is.Empty);
        Assert.That(run.Check(NoDefaultRule.File).Select(f => (f.Code, f.Message)), Is.EqualTo(new[] { ("UMP010199", "The size is 2.") }));
    }

    [Test]
    public void Option_with_no_default_and_no_value_is_not_valid() =>
        Assert.That(RuleOption.Number(LimitRule.MaxKey).IsValid(null), Is.False);

    [Test]
    public void Option_with_a_default_and_no_value_is_valid() =>
        Assert.That(RuleOption.Number(LimitRule.MaxKey, 5).IsValid(null), Is.True);

    [Test]
    public Task Option_that_is_not_valid_fails() =>
        Analyzer.VerifyFormatting(Rule, LongLine, "umpire.UMP010101.max = many",
            Option("UMP010101", "max", "a whole number more than 0", "many"));

    [Test]
    public Task Option_from_the_test_config_is_enough() =>
        Analyzer.VerifyFormatting(Rule, "class Abc { int valu{|#0:e; }|}", "umpire.UMP010101.max = 20",
            Analyzer.Diagnostic(Rule, 0, 24, 20));
}

public sealed class NoDefaultRule : KernelRule {
    public const string Key = "size";

    public static CodeFile File { get; } = new("Abc.cs", [13], [], [], [], [], []);

    public override int Number => 99;

    public override IReadOnlyList<RuleOption> Options { get; } = [RuleOption.Number(Key)];

    public override string Title => "A test rule with an option that has no default";

    public override string Message => "The size is {0}.";

    public static CodeRun Run(string config) => CodeRun.Start(
        new CompileProject("Abc", "", config, false, [], [], new ModelSettings(null, null, null, [], false)),
        (_, _) => true, [new NoDefaultRule()]);

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) =>
        options.Number(Key) is { } size ? [Found(new CodePlace(file.Path, 1, 1, file.Lines[0]), size)] : [];
}
