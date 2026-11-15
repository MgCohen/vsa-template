namespace TwoApps.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

public class SecondApp : App<Vsa<VsaHost>>;
