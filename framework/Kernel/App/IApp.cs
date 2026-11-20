namespace Umpire.Kernels;


public interface IApp {
    AppState State { get; }

    IServiceProvider? Services { get; }

    void Start(string[] args);

    void Stop();

    int Run(string[] args);
}
