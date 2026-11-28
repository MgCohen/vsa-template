namespace Umpire.Kernels;

public interface IApp {
    AppState State { get; }

    AppInstance Run(AppHost host);
}
