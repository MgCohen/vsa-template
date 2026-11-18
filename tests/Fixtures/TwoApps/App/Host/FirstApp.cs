namespace TwoApps.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernels;

public class FirstApp : App<Vsa<VsaHost>>;
