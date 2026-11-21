namespace Umpire.Architecture;

public interface IPartChain {
    IPartChain Add<T>() where T : Concept, new();

    PartGroup AddGroup<T>(string? folder = null) where T : Concept, new();

    PartGroup AddGroup<T>(Boundary boundary, string? folder = null) where T : Concept, new();

    PartGroup And<T>() where T : Concept, new();

    IPartChain InParentFolder();

    IPartChain OfType<T>(bool? required = null);

    IPartChain OfType(Type type, bool? required = null);

    IPartChain Named(Name name);
}
