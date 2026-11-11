namespace VSA.Module;

public interface IKeySource {
    IEnumerable<Type> KeysFor(Type genericParameter, IReadOnlyList<Type> candidates);
}
