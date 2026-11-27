namespace SampleApp.Host;

using Umpire.Hosts.Web;
using Umpire.Kernels;
using Umpire.Vsa.Composition;

public class SampleAppApp : App<VsaKernel, WebHost>;
