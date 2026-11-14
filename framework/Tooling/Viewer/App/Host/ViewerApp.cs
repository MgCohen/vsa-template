using VSA.Composition;
using VSA.Host;
using VSA.Kernel;

namespace App.Host;

public class ViewerApp : VsaApp<ViewerDefinition> {
    protected override IAppHost Host() => new WebVsaHost();
}
