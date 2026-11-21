namespace Umpire.Architecture;

public sealed class TraitRule {
    internal TraitRule(IReadOnlyList<Entry> subjects, Trait condition) {
        Subjects = subjects;
        Condition = condition;
    }

    internal IReadOnlyList<Entry> Subjects { get; }
    internal Trait Condition { get; private set; }

    public TraitRule And<T>() where T : Trait, new() {
        Condition &= new T();
        return this;
    }

    public override string ToString() => $"{string.Join(" and ", Subjects)} are {Condition}";
}
