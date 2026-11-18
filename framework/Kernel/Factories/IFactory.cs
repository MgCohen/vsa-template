namespace Umpire.Kernels;

public interface IFactory<out T> {
    T Create();
}
