namespace SampleApp.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Hosts.Web;
using Umpire.Kernels;

public class SampleAppApp : App<Vsa<WebVsaHost>>;
