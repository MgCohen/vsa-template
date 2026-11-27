namespace UmpireEngine;

using Operations.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Hosts.Console;
using Umpire.Kernels;

public sealed class EngineConsoleHost : ConsoleHost {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        var operations = services
            .Where(d => d.ServiceType == typeof(Operation) && d.ImplementationInstance is Operation)
            .ToList();
        foreach (var descriptor in operations) {
            services.Remove(descriptor);
            services.AddSingleton(WithReport((Operation)descriptor.ImplementationInstance!));
        }
    }

    static Operation WithReport(Operation operation) => operation with {
        Invoke = async (services, input, ct) => {
            var result = await operation.Invoke(services, input, ct).ConfigureAwait(false);
            return result.Value is JobReport report ? result with { ExitCode = ConsoleReport.Write(report) } : result;
        },
    };
}
