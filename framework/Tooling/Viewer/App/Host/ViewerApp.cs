using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Host;

namespace App.Host;

public class ViewerApp : VsaApp<VsaDefinition> {
    protected override IVsaHost Host() => new WebVsaHost();
}
