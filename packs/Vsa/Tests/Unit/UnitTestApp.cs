namespace Umpire.Vsa.Testing.Unit;

using Umpire.Kernels;

sealed class UnitTestApp<TKernel> : App<TKernel>
    where TKernel : Kernel, new() {
    internal IServiceProvider Provider =>
        Services ?? throw new InvalidOperationException("The app of the test does not run.");
}
