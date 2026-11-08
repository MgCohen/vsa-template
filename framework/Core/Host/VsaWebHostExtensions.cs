using VSA.Composition;

namespace VSA.Host;

public static class VsaWebHostExtensions {
    public static VsaBuilder UseWebHost(this VsaBuilder builder) => builder.UseHost(new WebVsaHost());
}
