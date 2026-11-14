using VSA.Kernel;

namespace VSA.Composition;

public static class VsaAppBuilderExtensions {
    public static AppBuilder Modules(this AppBuilder builder, params VSA.Module.Module[] modules) =>
        builder.ScanFor(typeof(VSA.Module.Module)).Anchors(modules);

    public static AppBuilder UseConsoleHost(this AppBuilder builder) => builder.UseHost(new ConsoleVsaHost());
}
