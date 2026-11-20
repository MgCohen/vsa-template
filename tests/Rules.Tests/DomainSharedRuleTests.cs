namespace Rules.Tests;

using Umpire.Vsa.Analyzers;

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
        """, Analyzer.Diagnostic(new ImmutableSharedTypeRule().Rule, 0, "CardId", "Name", "a property with a set accessor", "Use init in place of set"));

    [Test]
    public Task Field_that_is_not_readonly_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class CardId { public static int {|#0:Count|}; }
        """, Analyzer.Diagnostic(new ImmutableSharedTypeRule().Rule, 0, "CardId", "Count", "a field that is not readonly", "Make the field readonly"));

    [Test]
    public Task Record_struct_that_is_not_readonly_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public record struct Slot(int {|#0:Index|});
        """, Analyzer.Diagnostic(new ImmutableSharedTypeRule().Rule, 0, "Slot", "Index", "a property of a record struct that is not readonly", "Make it a readonly record struct"));

    [Test]
    public Task Field_event_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Card { public event Action? {|#0:Changed|}; }
        """, Analyzer.Diagnostic(new ImmutableSharedTypeRule().Rule, 0, "Card", "Changed", "a field event", "Remove the event: a Domain.Shared type holds only data"));

    [Test]
    public Task Type_in_another_project_passes() => Analyzer.VerifyIn("Cards.Domain", """
        public sealed class Card { public string Name { get; set; } = ""; public int count; }
        """);

    [Test]
    public Task Layer_property_selects_the_project() => Analyzer.VerifyIn("Cards.Domain.Shared", """
        public sealed class Card { public string Name { get; set; } = ""; }
        """, "build_property.UmpireLayer = Domain");

    [Test]
    public Task Layer_property_Domain_Shared_is_checked() => Analyzer.VerifyIn("Cards.Published", """
        public sealed class Card { public string {|#0:Name|} { get; set; } = ""; }
        """, "build_property.UmpireLayer = Domain/Shared",
        Analyzer.Diagnostic(new ImmutableSharedTypeRule().Rule, 0, "Card", "Name", "a property with a set accessor", "Use init in place of set"));
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
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "CardId", "IsEmpty", "a method"));

    [Test]
    public Task Record_override_written_by_hand_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardId(Guid Value) { public override string {|#0:ToString|}() => "card"; }
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "CardId", "ToString", "a method"));

    [Test]
    public Task Operator_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Rank(int Value) { public static Rank operator {|#0:+|}(Rank a, Rank b) => new(a.Value + b.Value); }
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "Rank", "operator +", "an operator"));

    [Test]
    public Task Conversion_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Rank(int Value) { public static implicit operator {|#0:int|}(Rank rank) => rank.Value; }
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "Rank", "implicit operator int", "a conversion"));

    [Test]
    public Task Constructor_with_a_body_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Rank { public int Value { get; } public {|#0:Rank|}(int value) { Value = value; } }
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "Rank", "Rank", "a constructor with a body"));

    [Test]
    public Task Computed_property_with_a_call_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record CardId(Guid Value) { public string {|#0:Text|} => Value.ToString(); }
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "CardId", "Text", "a property with a body that calls ToString"));

    [Test]
    public Task Indexer_with_a_call_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Hand(string Cards) { public string {|#0:this|}[int i] { get { return Cards.Substring(i, 1); } } }
        """, Analyzer.Diagnostic(new DataOnlySharedTypeRule().Rule, 0, "Hand", "this", "an indexer with a body that calls Substring"));

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
        Analyzer.Diagnostic(new SharedTypeBaseRule().Rule, 0, "CardKey", "Card.TypeKeys.TypeKey", "Card.TypeKeys.TypeKey"));

    [Test]
    public Task Marker_interface_of_VSA_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public readonly record struct Mana(int Value) : Umpire.Markers.IKeyMarker;
        """, ("Umpire.Markers", "namespace Umpire.Markers { public interface IKeyMarker { } }"));

    [Test]
    public Task Base_in_allowed_bases_passes() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed record Rank(int Value) : IEquatable<int>, IComparable<Rank> {
            public bool Equals(int other) => Value == other;
            public int CompareTo(Rank? other) => 0;
        }
        """, "umpire.UMP010207.allowed_bases = System.IEquatable`1, System.IComparable`1\ndotnet_diagnostic.UMP010206.severity = none");

    [Test]
    public Task Other_base_fails() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Pile : {|#0:System.Collections.Generic.List<int>|} { }
        public sealed record Rank(int Value) : {|#1:IEquatable<int>|} { public bool Equals(int other) => Value == other; }
        """, "umpire.UMP010207.allowed_bases = System.IComparable`1\ndotnet_diagnostic.UMP010206.severity = none",
        Analyzer.Diagnostic(new SharedTypeBaseRule().Rule, 0, "Pile", "System.Collections.Generic.List<int>", "System.Collections.Generic.List`1"),
        Analyzer.Diagnostic(new SharedTypeBaseRule().Rule, 1, "Rank", "System.IEquatable<int>", "System.IEquatable`1"));

    [Test]
    public Task Rule_option_wins_over_the_global_option() => Analyzer.VerifyIn(Shared.Assembly, """
        public sealed class Pile : {|#0:System.Collections.Generic.List<int>|} { }
        """, "umpire.allowed_bases = System.Collections.Generic.List`1\numpire.UMP010207.allowed_bases = System.IEquatable`1",
        Analyzer.Diagnostic(new SharedTypeBaseRule().Rule, 0, "Pile", "System.Collections.Generic.List<int>", "System.Collections.Generic.List`1"));

    [Test]
    public Task Type_in_another_project_passes() => Analyzer.VerifyIn("Cards.Domain", """
        public sealed class Pile : System.Collections.Generic.List<int> { }
        """);
}

public sealed class UmpireOptionsTests {
    static UmpireOptions Of(params (string Key, string Value)[] values) =>
        new("UMP010207", ConfigValues.Parse(string.Concat(values.Select(v => $"{v.Key} = \"{v.Value}\"\n"))),
            new SharedTypeBaseRule().Options);

    [Test]
    public void List_has_comma_and_semicolon_separators() =>
        Assert.That(Of(("umpire.UMP010207.allowed_bases", " A`1, B ;C;; ")).List("allowed_bases"), Is.EqualTo(new[] { "A`1", "B", "C" }));

    [Test]
    public void Rule_key_wins_over_the_global_key() =>
        Assert.That(Of(("umpire.allowed_bases", "A"), ("umpire.UMP010207.allowed_bases", "B")).List("allowed_bases"), Is.EqualTo(new[] { "B" }));

    [Test]
    public void Missing_key_gives_the_default_of_the_rule() =>
        Assert.That(Of().List("allowed_bases"), Is.Empty);

    [Test]
    public void Unknown_key_gives_null() =>
        Assert.That(Of().List("unknown"), Is.Null);
}
