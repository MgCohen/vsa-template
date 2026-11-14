using VSA.Composition;
using VSA.Kernel;

namespace VSA.Host;

public static class VsaWebHostExtensions {
    public static AppBuilder UseWebHost(this AppBuilder builder) => builder.UseHost(new WebVsaHost());
}
