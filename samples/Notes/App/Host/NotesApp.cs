using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Host;

namespace App.Host;

public class NotesApp : VsaApp<VsaDefinition> {
    protected override IVsaHost Host() => new WebVsaHost();
}
