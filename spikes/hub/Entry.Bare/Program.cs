using Spike.Core;
using Spike.Hosts.Console;

return await new BareApp().Run(new ConsoleHost(args));

sealed class BareApp : App<Kernel>;
