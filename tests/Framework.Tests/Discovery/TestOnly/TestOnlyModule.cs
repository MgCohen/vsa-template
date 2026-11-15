namespace Framework.Tests.Discovery.TestOnly;

using Microsoft.Extensions.DependencyInjection;
using VSA.Kernel;

public sealed class TestOnlyModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}
