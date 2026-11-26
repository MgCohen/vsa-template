namespace OpenHost {
    using Umpire.Kernels;
    using Umpire.Vsa.Composition;

    public sealed class OpenHostApp<THost> : App<VsaKernel, THost> where THost : AppHost, new();
}
