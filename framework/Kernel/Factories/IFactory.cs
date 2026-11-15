namespace VSA.Kernel;

public interface IFactory<out T> {
    T Create();
}
