namespace Framework.Tests.Host;

using Umpire.Kernel;

static class WebApps {
    public static WebTestApp OnTestServer(params Module[] modules) => new(modules);
}
