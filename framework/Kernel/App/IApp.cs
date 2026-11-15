namespace Umpire.Kernel;

using Umpire.Markers;

[RequireImplementors]
public interface IApp {
    bool IsRunning { get; }

    IServiceProvider? Services { get; }

    int Run(string[] args);

    void Stop();
}
