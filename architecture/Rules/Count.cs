namespace Umpire.Architecture;

public sealed class Count {
    internal Count(IReadOnlyList<Entry> subjects, Type counted, Depth? depth, bool types = false) {
        Subjects = subjects;
        Counted = counted;
        Depth = depth;
        Types = types;
    }

    internal IReadOnlyList<Entry> Subjects { get; }
    internal Type Counted { get; }
    internal Depth? Depth { get; }
    internal bool Types { get; }
    internal int? Min { get; private set; }
    internal int? Max { get; private set; }
    internal bool OneKind { get; private set; }
    internal bool HasLimit => Min is not null || Max is not null || OneKind;

    public Count AtLeast(int count) {
        Min = count;
        return this;
    }

    public Count AtMost(int count) {
        Max = count;
        return this;
    }

    public Count Exactly(int count) {
        Min = count;
        Max = count;
        return this;
    }

    public Count OfOneKind() {
        OneKind = true;
        return this;
    }

    public override string ToString() =>
        $"{string.Join(" and ", Subjects)} has {(Types ? $"types of {Counted.Name}" : Concept.Display(Counted))}{(Depth is null ? "" : $"({Depth})")} {Limits.Need(Min, Max, OneKind)}";
}
