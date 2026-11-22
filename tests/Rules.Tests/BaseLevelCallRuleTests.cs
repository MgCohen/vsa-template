using Tests.Shared;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed class BaseLevelCallRuleTests {
    static readonly Microsoft.CodeAnalysis.DiagnosticDescriptor Rule = new BaseLevelCallRule().Rule;

    [Test]
    public Task A_base_call_in_the_rules_of_a_derived_architecture_fails() => Analyzer.VerifyFormatting(Rule, """
        using Umpire.Architecture;

        public class First : Architecture {
            protected override void Define(Part root) { }
        }

        public sealed class Second : First {
            protected override void Rules(RuleBook rules) {
                {|#0:base.Rules(rules)|};
            }
        }
        """, "", Analyzer.Diagnostic(Rule, 0, "Second", "Rules"));

    [Test]
    public Task A_base_call_in_the_define_of_a_block_record_fails() => Analyzer.VerifyFormatting(Rule, """
        using Umpire.Architecture;
        using Umpire.Architecture.Blocks;

        public record Shelf : Block {
            protected override void Define(Part part) {
                {|#0:base.Define(part)|};
            }
        }
        """, "", Analyzer.Diagnostic(Rule, 0, "Shelf", "Define"));

    [Test]
    public Task A_level_with_no_base_call_passes() => Analyzer.VerifyFormatting(Rule, """
        using Umpire.Architecture;

        public sealed class Only : Architecture {
            protected override void Define(Part root) { }

            protected override void Rules(RuleBook rules) { }
        }
        """, "");

    [Test]
    public Task A_base_call_in_a_class_that_is_not_a_definition_passes() => Analyzer.VerifyFormatting(Rule, """
        public class Setup {
            protected virtual void Configure(int value) { }
        }

        public sealed class LocalSetup : Setup {
            protected override void Configure(int value) {
                base.Configure(value);
            }
        }
        """, "");
}
