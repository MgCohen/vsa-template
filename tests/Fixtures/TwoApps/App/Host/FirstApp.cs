namespace TwoApps.App.Host;

using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Kernel;

public class FirstApp : App<Vsa<VsaHost>>;
