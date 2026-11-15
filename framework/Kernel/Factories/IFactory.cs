namespace Umpire.Kernel;

public interface IFactory<out T> {
    T Create();
}
