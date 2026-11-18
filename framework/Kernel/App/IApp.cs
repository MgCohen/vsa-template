namespace Umpire.Kernels;

using Umpire.Markers;

[RequireImplementors]
public interface IApp {
    bool IsRunning { get; }

    IServiceProvider? Services { get; }

    void Start(string[] args);

    int Run(string[] args);

    void Stop();
}
