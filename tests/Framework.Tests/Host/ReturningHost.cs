namespace Framework.Tests.Host;

using Microsoft.Extensions.Hosting;
using VSA.Composition;

public sealed class ReturningHost : VsaHost {
    public override int Run(IHost app) {
        app.StartAsync().GetAwaiter().GetResult();
        return 0;
    }

    public override void Stop(IHost app) {
        app.StopAsync().GetAwaiter().GetResult();
        app.Dispose();
    }
}
