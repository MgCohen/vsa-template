namespace VSA.Kernel;

public interface IFactory<out T> {
    T Create();
}

public interface IFactory<in TArg, out T> {
    T Create(TArg arg);
}
