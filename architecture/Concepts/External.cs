namespace Umpire.Architecture;

public abstract record External : Concept {
    public abstract string Assembly { get; }
}
