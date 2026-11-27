namespace Umpire.Kernels;

public abstract class App<TKernel> : App, IAppEntry<TKernel> where TKernel : Kernel, new() {
    readonly TKernel kernel = new();

    public sealed override Kernel Kernel => kernel;
}
