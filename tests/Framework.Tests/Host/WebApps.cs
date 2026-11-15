namespace Framework.Tests.Host;

using VSA.Kernel;

static class WebApps {
    public static WebTestApp OnTestServer(params Module[] modules) => new(modules);
}
