namespace VSA.Architecture;

public abstract record External : Concept {
    public abstract string Assembly { get; }
}
