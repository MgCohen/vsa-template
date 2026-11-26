namespace Spike.Kernels;

public abstract class App<TKernel> : App where TKernel : Kernel, new() {
    readonly TKernel kernel = new();

    public sealed override Kernel Kernel => kernel;
}
