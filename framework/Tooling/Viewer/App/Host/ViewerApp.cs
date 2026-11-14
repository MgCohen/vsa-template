using VSA.Composition;
using VSA.Host;

namespace App.Host;

public class ViewerApp : VsaApp<ViewerDefinition> {
    protected override IVsaHost Host() => new WebVsaHost();
}
