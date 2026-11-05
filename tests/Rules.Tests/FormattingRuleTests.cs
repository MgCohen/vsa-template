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

    [Test]
    public Task Default_limit_is_120() => Analyzer.VerifyFormatting(Rule, $$"""
        class A { string S = "{{new string('x', 120 - 26)}}"; }
        """, "");
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

    // A global option applies to each rule that reads the key, so vsa.max also sets this limit.
    [Test]
    public Task Global_max_option_applies() =>
        Analyzer.VerifyFormatting(Rule, Method, "vsa.max = 3", Analyzer.Diagnostic(Rule, 0, "M", 4, 3));
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
        Analyzer.Diagnostic(Rule, 0, name, group, previous, string.Join(", ", MemberOrderRule.DefaultKindOrder),
            string.Join(", ", MemberOrderRule.DefaultAccessOrder), order);

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

// VSA.globalconfig shows each option of the formatting rules. Its values are the defaults in the code of the rules.
public sealed class FormattingDefaultsTests {
    [Test]
    public void Global_config_has_the_defaults_of_the_rules() {
        var config = File.ReadAllLines(Repo.Path("framework", "Sdk", "Sdk", "VSA.globalconfig"))
            .Where(line => line.StartsWith("vsa.", StringComparison.Ordinal))
            .Select(line => line.Split('=', 2))
            .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());
        var defaults = new Dictionary<string, string> {
            ["vsa.VSA1008.max"] = $"{new LineLengthRule().DefaultMax}",
            ["vsa.VSA1009.max"] = $"{new MethodLengthRule().DefaultMax}",
            ["vsa.VSA1010.max"] = $"{new FileLengthRule().DefaultMax}",
            ["vsa.VSA1011.allow_same_name"] = $"{OneTypePerFileRule.DefaultAllowSameName}".ToLowerInvariant(),
            ["vsa.VSA1012.kind_order"] = string.Join(", ", MemberOrderRule.DefaultKindOrder),
            ["vsa.VSA1012.access_order"] = string.Join(", ", MemberOrderRule.DefaultAccessOrder),
            ["vsa.VSA1012.static_first"] = $"{MemberOrderRule.DefaultStaticFirst}".ToLowerInvariant(),
        };
        Assert.That(config, Is.SupersetOf(defaults));
    }
}
