namespace Umpire.Architecture;

public sealed class Match {
    internal Match(IReadOnlyList<Entry> subjects, Type taken) {
        Subjects = subjects;
        Taken = taken;
    }

    internal IReadOnlyList<Entry> Subjects { get; }
    internal Type Taken { get; }
    internal Type? Group { get; private set; }

    public Match InSame<T>() where T : Concept {
        Group = typeof(T);
        return this;
    }

    public override string ToString() =>
        $"{string.Join(" and ", Subjects)} takes {Concept.Display(Taken)}{(Group is null ? "" : $" in same {Concept.Display(Group)}")}";
}
