namespace VSA.Kernel;

using VSA.Analyzers.Markers;

[RequireImplementors]
public interface IApp {
    bool IsRunning { get; }

    IServiceProvider? Services { get; }

    int Run(string[] args);

    void Stop();
}
