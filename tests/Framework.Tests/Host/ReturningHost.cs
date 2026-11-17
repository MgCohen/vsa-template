namespace Framework.Tests.Host;

using Microsoft.Extensions.Hosting;
using Umpire.Vsa.Composition;

public sealed class ReturningHost : VsaHost {
    public override int Run() {
        Built.StartAsync().GetAwaiter().GetResult();
        return 0;
    }

    public override void Stop() {
        Built.StopAsync().GetAwaiter().GetResult();
        Built.Dispose();
    }
}
