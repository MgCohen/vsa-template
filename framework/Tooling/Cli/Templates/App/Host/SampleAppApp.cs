using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Host;
using VSA.Kernel;

namespace App.Host;

public class SampleAppApp : VsaApp<VsaDefinition> {
    protected override IAppHost Host() => new WebVsaHost();
}
