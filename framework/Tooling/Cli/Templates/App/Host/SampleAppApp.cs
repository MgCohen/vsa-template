namespace SampleApp.App.Host;

using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Host;
using VSA.Kernel;

public class SampleAppApp : VsaApp<VsaArchitecture> {
    protected override IAppHost Host() => new WebVsaHost();
}
