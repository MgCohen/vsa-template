namespace UmpireEngine;

using Facts.Infrastructure;
using Operations.Contracts.Requests;
using Operations.Domain.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umpire.Hosts.Console;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Composition;

public sealed class EngineConsoleHost : ConsoleHost {
    static readonly string[] JobWords = ["jobs", "build"];
    ReportMode? mismatch;

    public override IServiceProvider Build(string[] args, App app) {
        if (!HookMismatch.HasNoJobWord(args)) {
            return base.Build(args, app);
        }
        mismatch = HookMismatch.Mode(HostArguments.Read(args));
        return base.Build([.. JobWords], app);
    }

    public override void Start(IServiceProvider services, AppStop stop) {
        if (mismatch is { } mode) {
            var exitCode = ConsoleReport.Write(mode, [HookMismatch.Finding]);
            Task.Run(() => stop.Request(exitCode));
            return;
        }
        base.Start(services, stop);
    }

    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        services.AddSingleton<IFactSource<DefinitionQuery, IDefinitionLoader>, DefinitionSource>();
        services.Replace(ServiceDescriptor.Singleton<IOperationConverter>(
            RequestConverter.Of(types, request => typeof(JobRequest).IsAssignableFrom(request))));
    }

    protected override int ExitCodeOf(IMessageResult result) =>
        result.Value is JobReport report ? ConsoleReport.Write(report) : base.ExitCodeOf(result);
}
