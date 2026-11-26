namespace SpikeApp {
    using Umpire.Kernels;
    using Umpire.Vsa.Composition;

    public sealed class SpikeApp<THost> : App<VsaKernel, THost> where THost : AppHost, new();
}
