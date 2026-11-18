namespace Framework.Tests.Host;

using Umpire.Kernels;

static class WebApps {
    public static WebTestApp OnTestServer(params Module[] modules) => new(modules);
}
