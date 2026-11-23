namespace EngineCore.Tests;

using Umpire.Architecture;

public record Lib : External {
    public override string Assembly => "Lib";
}

public record OtherLib : External {
    public override string Assembly => "OtherLib";
}

public record Group : Concept;

public record Child : Concept;

public sealed class InlineArchitecture(Action<Part> define, Action<RuleBook>? rules = null) : Architecture {
    protected override void Define(Part block, Part kernel) {
        define(block);
    }

    protected override void Rules(RuleBook book) {
        rules?.Invoke(book);
    }
}

public sealed class ExternalTests {
    [Test]
    public void A_rule_that_names_an_external_gives_it_its_part() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(block => block.Add<Child>(), rules => rules.For<Child>().Uses<Lib>()));

        Assert.That(resolved.ExternalPart("Lib")?.Name, Is.EqualTo(nameof(Lib)));
    }

    [Test]
    public void An_external_that_no_rule_names_has_no_part() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(block => block.Add<Child>(), rules => rules.For<Child>().Uses<Lib>()));

        Assert.That(resolved.ExternalPart("OtherLib"), Is.Null);
    }

    [Test]
    public void Two_rules_that_name_one_external_give_one_part() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(
            block => block.Add<Child>().And<Group>(),
            rules => {
                rules.For<Child>().Uses<Lib>();
                rules.For<Group>().Denies<Lib>();
            }));

        Assert.That(resolved.Parts.Count(p => p.Concept is Lib), Is.EqualTo(1));
    }

    [Test]
    public void Add_of_an_external_throws() {
        Blocks(block => block.Add<Lib>(), "Define: Add<Lib>() adds an External. An External is a DLL, not a folder");
    }

    [Test]
    public void AddGroup_of_an_external_throws() {
        Blocks(block => block.AddGroup<Lib>(), "Define: AddGroup<Lib>() adds an External.");
    }

    [Test]
    public void An_external_as_the_subject_of_a_rule_throws() {
        var definition = new InlineArchitecture(block => block.Add<Child>(), rules => rules.For<Lib>().Uses<Child>());

        Assert.That(() => new ResolvedArchitecture(definition),
            Throws.InvalidOperationException.With.Message.Contains("Lib is an External. An External is only the target of a rule"));
    }

    static void Blocks(Action<Part> define, string message) {
        var definition = new InlineArchitecture(define);

        Assert.That(() => new ResolvedArchitecture(definition), Throws.InvalidOperationException.With.Message.Contains(message));
    }
}
