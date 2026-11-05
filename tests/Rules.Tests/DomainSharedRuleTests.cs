using VSA.Analyzers;

namespace Rules.Tests;

// The rules of a Domain.Shared project: VSA1005, VSA1006 and VSA1007. The test code is in Cards.Domain.Shared,
// so all three rules check each type.
static class Shared {
    public const string Assembly = "Cards.Domain.Shared";
}

public sealed class ImmutableSharedTypeRuleTests {
    [Test]
    public Task Init_property_and_readonly_field_pass() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class CardId {
            public const int Size = 16;
            public static readonly CardId Empty = new();
            readonly int value;
            public string Name { get; init; } = "";
            public int Value => value;
        }
        """);

    [Test]
    public Task Positional_record_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardId(Guid Value);
        public readonly record struct Slot(int Index);
        """);

    [Test]
    public Task Set_property_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class CardId { public string {|#0:Name|} { get; set; } = ""; }
        """, Analyzer.Diagnostic(ImmutableSharedTypeRule.Descriptor, 0, "CardId", "Name", "a property with a set accessor", "Use init in place of set"));

    [Test]
    public Task Field_that_is_not_readonly_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class CardId { public static int {|#0:Count|}; }
        """, Analyzer.Diagnostic(ImmutableSharedTypeRule.Descriptor, 0, "CardId", "Count", "a field that is not readonly", "Make the field readonly"));

    [Test]
    public Task Record_struct_that_is_not_readonly_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public record struct Slot(int {|#0:Index|});
        """, Analyzer.Diagnostic(ImmutableSharedTypeRule.Descriptor, 0, "Slot", "Index", "a property of a record struct that is not readonly", "Make it a readonly record struct"));

    [Test]
    public Task Field_event_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Card { public event Action? {|#0:Changed|}; }
        """, Analyzer.Diagnostic(ImmutableSharedTypeRule.Descriptor, 0, "Card", "Changed", "a field event", "Remove the event: a Domain.Shared type holds only data"));

    // A type outside a Domain.Shared project is not checked.
    [Test]
    public Task Type_in_another_project_passes() => Analyzer.VerifyIn("Cards.Domain", """
        public sealed class Card { public string Name { get; set; } = ""; public int count; }
        """);

    // The MSBuild property VsaLayer wins over the assembly name.
    [Test]
    public Task Layer_property_selects_the_project() => Analyzer.VerifyIn("Cards.Domain.Shared", """
        public sealed class Card { public string Name { get; set; } = ""; }
        """, "build_property.VsaLayer = Domain");

    [Test]
    public Task Layer_property_Domain_Shared_is_checked() => Analyzer.VerifyIn("Cards.Published", """
        public sealed class Card { public string {|#0:Name|} { get; set; } = ""; }
        """, "build_property.VsaLayer = Domain/Shared",
        Analyzer.Diagnostic(ImmutableSharedTypeRule.Descriptor, 0, "Card", "Name", "a property with a set accessor", "Use init in place of set"));
}

public sealed class DataOnlySharedTypeRuleTests {
    [Test]
    public Task Data_type_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public record Key(string Name);
        public sealed record CardKey(string Name, int Rank) : Key(Name) {
            public static readonly CardKey Ace = new(string.Concat("a", "ce"), 1);
            public string Label => Name + Rank;
            public int Twice => Rank * 2;
            public string Title => nameof(Rank);
        }
        """);

    [Test]
    public Task Constructor_with_only_an_initializer_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public class Key { public Key(string name) { } public Key() : this("") { } }
        public sealed class CardKey : Key { public CardKey(string name) : base(name) { } }
        """);

    [Test]
    public Task Method_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardId(Guid Value) { public bool {|#0:IsEmpty|}() => Value == Guid.Empty; }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "CardId", "IsEmpty", "a method"));

    [Test]
    public Task Record_override_written_by_hand_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardId(Guid Value) { public override string {|#0:ToString|}() => "card"; }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "CardId", "ToString", "a method"));

    [Test]
    public Task Operator_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Rank(int Value) { public static Rank operator {|#0:+|}(Rank a, Rank b) => new(a.Value + b.Value); }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "Rank", "operator +", "an operator"));

    [Test]
    public Task Conversion_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Rank(int Value) { public static implicit operator {|#0:int|}(Rank rank) => rank.Value; }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "Rank", "implicit operator int", "a conversion"));

    [Test]
    public Task Constructor_with_a_body_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Rank { public int Value { get; } public {|#0:Rank|}(int value) { Value = value; } }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "Rank", "Rank", "a constructor with a body"));

    [Test]
    public Task Computed_property_with_a_call_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardId(Guid Value) { public string {|#0:Text|} => Value.ToString(); }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "CardId", "Text", "a property with a body that calls ToString"));

    [Test]
    public Task Indexer_with_a_call_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Hand(string Cards) { public string {|#0:this|}[int i] { get { return Cards.Substring(i, 1); } } }
        """, Analyzer.Diagnostic(DataOnlySharedTypeRule.Descriptor, 0, "Hand", "this", "an indexer with a body that calls Substring"));

    [Test]
    public Task Type_in_another_project_passes() => Analyzer.VerifyIn("Cards.Domain", """
        public sealed record CardId(Guid Value) { public bool IsEmpty() => Value == Guid.Empty; public string Text => Value.ToString(); }
        """);
}

public sealed class SharedTypeBaseRuleTests {
    const string Other = """
        namespace Card.TypeKeys { public record TypeKey(string Name); public interface IKeyed { } }
        """;

    [Test]
    public Task Base_in_the_same_project_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public interface IKeyed { }
        public record Key(string Name) : IKeyed;
        public sealed record CardKey(string Name) : Key(Name), IKeyed;
        public enum Suit : byte { Hearts }
        """);

    [Test]
    public Task Base_from_another_Domain_Shared_assembly_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardKey(string Name) : Card.TypeKeys.TypeKey(Name), Card.TypeKeys.IKeyed;
        """, ("Keys.Domain.Shared", Other));

    [Test]
    public Task Base_from_another_assembly_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardKey(string Name) : {|#0:Card.TypeKeys.TypeKey|}(Name);
        """, ("Keys.Domain", Other),
        Analyzer.Diagnostic(SharedTypeBaseRule.Descriptor, 0, "CardKey", "Card.TypeKeys.TypeKey", "Card.TypeKeys.TypeKey"));

    [Test]
    public Task Base_in_allowed_bases_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Rank(int Value) : IEquatable<int>, IComparable<Rank> {
            public bool Equals(int other) => Value == other;
            public int CompareTo(Rank? other) => 0;
        }
        """, "vsa.VSA1007.allowed_bases = System.IEquatable`1, System.IComparable`1\ndotnet_diagnostic.VSA1006.severity = none");

    [Test]
    public Task Other_base_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Pile : {|#0:System.Collections.Generic.List<int>|} { }
        public sealed record Rank(int Value) : {|#1:IEquatable<int>|} { public bool Equals(int other) => Value == other; }
        """, "vsa.VSA1007.allowed_bases = System.IComparable`1\ndotnet_diagnostic.VSA1006.severity = none",
        Analyzer.Diagnostic(SharedTypeBaseRule.Descriptor, 0, "Pile", "System.Collections.Generic.List<int>", "System.Collections.Generic.List`1"),
        Analyzer.Diagnostic(SharedTypeBaseRule.Descriptor, 1, "Rank", "System.IEquatable<int>", "System.IEquatable`1"));

    // A rule key wins over a global key.
    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Pile : {|#0:System.Collections.Generic.List<int>|} { }
        """, "vsa.allowed_bases = System.Collections.Generic.List`1\nvsa.VSA1007.allowed_bases = System.IEquatable`1",
        Analyzer.Diagnostic(SharedTypeBaseRule.Descriptor, 0, "Pile", "System.Collections.Generic.List<int>", "System.Collections.Generic.List`1"));

    [Test]
    public Task Type_in_another_project_passes() => Analyzer.VerifyIn("Cards.Domain", """
        public sealed class Pile : System.Collections.Generic.List<int> { }
        """);
}

// A list option. A config file line ends at a semicolon, so a config file uses a comma. Other sources can use a semicolon.
public sealed class VsaOptionsTests {
    sealed class Config(Dictionary<string, string> values) : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions {
        public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value) =>
            values.TryGetValue(key, out value);
    }

    static VsaOptions Of(params (string Key, string Value)[] values) =>
        new("VSA1007", new Config(values.ToDictionary(v => v.Key, v => v.Value)));

    [Test]
    public void List_has_comma_and_semicolon_separators() =>
        Assert.That(Of(("vsa.VSA1007.allowed_bases", " A`1, B ;C;; ")).GetList("allowed_bases"), Is.EqualTo(new[] { "A`1", "B", "C" }));

    [Test]
    public void Rule_key_wins_over_the_global_key() =>
        Assert.That(Of(("vsa.allowed_bases", "A"), ("vsa.VSA1007.allowed_bases", "B")).GetList("allowed_bases"), Is.EqualTo(new[] { "B" }));

    [Test]
    public void Missing_key_gives_no_items() =>
        Assert.That(Of().GetList("allowed_bases"), Is.Empty);
}
