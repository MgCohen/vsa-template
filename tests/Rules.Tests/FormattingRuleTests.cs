using Tests.Shared;
using VSA.Analyzers;

namespace Rules.Tests;

// The formatting rules VSA1008 to VSA1012. Each test turns on only its own rule. The limits are small, so the code stays short.
public sealed class LineLengthRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = LineLengthRule.Descriptor;

    [Test]
    public Task Line_at_the_limit_passes() => Analyzer.VerifyFormatting(Rule, """
        class Abc { int v; }
        """, "vsa.VSA1008.max = 20");

    [Test]
    public Task Line_over_the_limit_fails() => Analyzer.VerifyFormatting(Rule, """
        class Abc { int valu{|#0:e; }|}
        """, "vsa.VSA1008.max = 20", Analyzer.Diagnostic(Rule, 0, 24, 20));

    // The limit of VSA.Rules.globalconfig applies when the test gives no limit.
    [Test]
    public Task Limit_comes_from_the_rules_config() {
        var max = int.Parse(RulesConfig.Value("vsa.VSA1008.max"));
        var comment = "// " + new string('x', max - 3);
        var source = string.Join("\n", "class A { }", comment, comment + "{|#0:x|}");
        return Analyzer.VerifyFormatting(Rule, source, "", Analyzer.Diagnostic(Rule, 0, max + 1, max));
    }
}

public sealed class MethodLengthRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = MethodLengthRule.Descriptor;

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
        Analyzer.VerifyFormatting(Rule, Method.Replace("{|#0:M|}", "M"), "vsa.VSA1009.max = 4");

    [Test]
    public Task Method_over_the_limit_fails() =>
        Analyzer.VerifyFormatting(Rule, Method, "vsa.VSA1009.max = 3", Analyzer.Diagnostic(Rule, 0, "M", 4, 3));

    [Test]
    public Task Constructor_over_the_limit_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
            int a;
            {|#0:A|}() {
                a = 1;
            }
        }
        """, "vsa.VSA1009.max = 2", Analyzer.Diagnostic(Rule, 0, "Constructor A", 3, 2));

    // A global key applies to each rule that reads it, if no rule key is set. VSA.Rules.globalconfig sets the rule keys.
    [Test]
    public Task Global_max_option_applies() =>
        Analyzer.VerifyFormattingWithoutRulesConfig(Rule, Method, "vsa.max = 3", Analyzer.Diagnostic(Rule, 0, "M", 4, 3));
}

public sealed class FileLengthRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = FileLengthRule.Descriptor;

    [Test]
    public Task File_at_the_limit_passes() => Analyzer.VerifyFormatting(Rule, """
        class A {
        }
        // end
        """, "vsa.VSA1010.max = 3");

    [Test]
    public Task File_over_the_limit_fails() => Analyzer.VerifyFormatting(Rule, """
        class A {
        }
        // one
        {|#0:// two|}
        """, "vsa.VSA1010.max = 3", Analyzer.Diagnostic(Rule, 0, 4, 3));
}

public sealed class OneTypePerFileRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = OneTypePerFileRule.Descriptor;

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

    // Module and Module<TSettings> share a file by default.
    [Test]
    public Task Types_with_the_same_name_pass() => Analyzer.VerifyFormatting(Rule, """
        abstract class Module { }
        abstract class Module<TSettings> : Module { }
        """, "");

    [Test]
    public Task Option_turns_off_the_same_name_exception() => Analyzer.VerifyFormatting(Rule, """
        abstract class Module { }
        abstract class {|#0:Module|}<TSettings> : Module { }
        """, "vsa.VSA1011.allow_same_name = false", Analyzer.Diagnostic(Rule, 0, "Module", "Module"));
}

public sealed class MemberOrderRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = MemberOrderRule.Descriptor;

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Diagnostic(string name, string group, string previous,
        string order = "static before instance") =>
        Analyzer.Diagnostic(Rule, 0, name, group, previous, RulesConfig.Value("vsa.VSA1012.kind_order"),
            RulesConfig.Value("vsa.VSA1012.access_order"), order);

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

    // Interface members are public by default.
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
        """, "vsa.VSA1012.kind_order = method, field");

    [Test]
    public Task Static_first_option_changes_the_order() => Analyzer.VerifyFormatting(Rule, """
        class A {
            static void M() { }
            void {|#0:N|}() { }
        }
        """, "vsa.VSA1012.static_first = false",
        Diagnostic("N", "private method", "private static method", "instance before static"));
}

public sealed class CommentRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = CommentRule.Descriptor;

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

// VSA1000: each option of a rule has a valid value, else the rule does not run.
public sealed class RuleOptionTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = LineLengthRule.Descriptor;

    const string LongLine = "class Abc { int value; }";

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Option(string rule, string key, string kind, string value) =>
        new Microsoft.CodeAnalysis.Testing.DiagnosticResult(VsaAnalyzer.OptionDescriptor).WithArguments(rule, key, kind, value);

    [Test]
    public Task Missing_option_fails_and_the_rule_does_not_run() =>
        Analyzer.VerifyFormattingWithoutRulesConfig(Rule, LongLine, "",
            Option("VSA1008", "max", "a whole number more than 0", "missing"));

    [Test]
    public Task Option_that_is_not_valid_fails() =>
        Analyzer.VerifyFormatting(Rule, LongLine, "vsa.VSA1008.max = many",
            Option("VSA1008", "max", "a whole number more than 0", "many"));

    [Test]
    public Task Option_from_the_test_config_is_enough() =>
        Analyzer.VerifyFormattingWithoutRulesConfig(Rule, "class Abc { int valu{|#0:e; }|}", "vsa.VSA1008.max = 20",
            Analyzer.Diagnostic(Rule, 0, 24, 20));

    // Each option of each rule is in VSA.Rules.globalconfig, with a valid value.
    [Test]
    public void Rules_config_has_each_option() {
        var missing = VsaAnalyzer.Rules.SelectMany(rule => rule.RequiredOptions
            .Where(option => !option.IsValid(TryValue($"vsa.{rule.Id}.{option.Key}")))
            .Select(option => $"vsa.{rule.Id}.{option.Key}"));
        Assert.That(missing, Is.Empty, RulesConfig.Path);
    }

    static string? TryValue(string key) {
        try {
            return RulesConfig.Value(key);
        } catch (InvalidOperationException) {
            return null;
        }
    }
}
