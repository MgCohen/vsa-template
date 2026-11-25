namespace Rules.Tests;

using Umpire.Vsa.Analyzers;

static class Shared {
    public const string Assembly = "Cards.Domain.Shared";
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

public sealed class UmpireOptionsTests {
    static UmpireOptions Of(params (string Key, string Value)[] values) =>
        new("UMP019999", ConfigValues.Parse(string.Concat(values.Select(v => $"{v.Key} = \"{v.Value}\"\n"))),
            [UmpireOption.List("names", [])]);

    [Test]
    public void List_has_comma_and_semicolon_separators() =>
        Assert.That(Of(("umpire.UMP019999.names", " A`1, B ;C;; ")).List("names"), Is.EqualTo(new[] { "A`1", "B", "C" }));

    [Test]
    public void Rule_key_wins_over_the_global_key() =>
        Assert.That(Of(("umpire.names", "A"), ("umpire.UMP019999.names", "B")).List("names"), Is.EqualTo(new[] { "B" }));

    [Test]
    public void Missing_key_gives_the_default_of_the_rule() =>
        Assert.That(Of().List("names"), Is.Empty);

    [Test]
    public void Unknown_key_gives_null() =>
        Assert.That(Of().List("unknown"), Is.Null);
}
