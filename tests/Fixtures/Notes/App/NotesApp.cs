namespace Notes.Host;

using Umpire.Hosts.Web;
using Umpire.Kernels;
using Umpire.Vsa;

public class NotesApp : App<Vsa<WebHost>>;
