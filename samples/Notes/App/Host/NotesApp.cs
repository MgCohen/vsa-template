namespace Notes.App.Host;

using VSA.Composition;
using VSA.Host;
using VSA.Kernel;

public class NotesApp : App<Vsa<WebVsaHost>>;
