using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VSA.Handlers.Commands;
using VSA.Module;

namespace VSA.Composition;

sealed class ConsoleVsaHost : IVsaHost {
    public IHostApplicationBuilder CreateBuilder(string[] args) => Host.CreateApplicationBuilder(args);

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
        var entries = types.Implementations(typeof(CommandHandler<,>))
            .Select(handler => handler.Service.GetGenericArguments()[0])
            .Where(ConsoleEntry.IsConsoleCommand)
            .Distinct()
            .ToList();
        if (entries.Count != 1) {
            throw new InvalidOperationException(entries.Count == 0
                ? "The console host found no entry command. Add a command that implements IConsoleCommand<TResult>, and its handler."
                : $"The console host found more than one entry command: {string.Join(", ", entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal))}. Keep one.");
        }
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        builder.Services.AddSingleton<ConsoleExit>();
        builder.Services.AddHostedService(services => new ConsoleEntry(
            entries[0],
            services,
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<IHostApplicationLifetime>(),
            services.GetRequiredService<ConsoleExit>()));
    }

    public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

    public int Run(IHost app) {
        var exit = app.Services.GetRequiredService<ConsoleExit>();
        app.Run();
        return exit.Code;
    }

    public void Stop(IHost app) => app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
}
