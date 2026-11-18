namespace SampleApp.App.Host;

using Umpire.Hosts.Web;
using Umpire.Kernels;
using Umpire.Vsa;

public class SampleAppApp : App<Vsa<WebHost>>;
