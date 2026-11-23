namespace Umpire.Architecture;

using Umpire.Architecture.Blocks;

public sealed class PartFind {
    readonly Part start;
    readonly Type concept;
    readonly List<Type> under = [];

    internal PartFind(Part start, Type concept) {
        this.start = start.SelfAndAncestors().Last();
        this.concept = concept;
    }

    public PartFind Under<T>() where T : Concept {
        under.Add(typeof(T));
        return this;
    }

    public PartGroup Add<T>() where T : Concept, new() => new(Found().Select(part => part.Overlay(found => found.Add<T>())));

    public PartGroup ExtendedBy<T>() where T : Block, new() {
        var body = new T();
        var found = Found();
        foreach (var part in found) {
            part.StackBody(body);
        }
        start.AddExtension(new Extension(body, concept, [.. under]));
        return new(found);
    }

    IReadOnlyList<Part> Found() {
        var found = start.Walk().Where(part => part.Is(concept) && under.All(u => part.Ancestors().Any(a => a.Is(u)))).ToList();
        if (found.Count == 0) {
            throw new InvalidOperationException(
                $"Define: {Text()} finds no placed {Concept.Display(concept)}. Place it first, or remove the overlay.");
        }
        return found;
    }

    string Text() =>
        $"For<{Concept.Display(concept)}>(){string.Concat(under.Select(u => $".Under<{Concept.Display(u)}>()"))}";
}
