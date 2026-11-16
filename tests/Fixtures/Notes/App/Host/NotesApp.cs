namespace Notes.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Hosts.Web;
using Umpire.Kernel;

public class NotesApp : App<Vsa<WebVsaHost>>;
