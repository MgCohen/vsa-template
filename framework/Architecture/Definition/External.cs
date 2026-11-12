namespace VSA.Architecture;

public abstract record External : Layer {
    public abstract string Assembly { get; }
}
