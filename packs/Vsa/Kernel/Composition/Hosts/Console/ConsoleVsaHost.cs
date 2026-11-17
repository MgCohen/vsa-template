namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernel;

public sealed class ConsoleVsaHost : VsaHost {
    public override int Run() {
        var exit = Built.Services.GetRequiredService<ConsoleExit>();
        Built.Run();
        return exit.Code;
    }

    protected override void Install(IServiceCollection services, AppTypes types) {
        base.Install(services, types);
        var entry = EntryCommand(types);
        services.AddLogging(logging => logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning));
        services.AddSingleton<ConsoleExit>();
        services.AddHostedService(provider => new ConsoleEntry(
            entry,
            provider,
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IHostApplicationLifetime>(),
            provider.GetRequiredService<ConsoleExit>()));
    }

    static Type EntryCommand(AppTypes types) {
        var entries = types.Implementations(typeof(CommandHandler<,>))
            .Select(handler => handler.Service.GetGenericArguments()[0])
            .Where(ConsoleEntry.IsConsoleCommand)
            .Distinct()
            .ToList();
        var names = string.Join(", ", entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal));
        return entries.Count == 1 ? entries[0] : throw new InvalidOperationException(entries.Count == 0
            ? "The console host found no entry command. "
                + "Add a command that implements IConsoleCommand<TResult>, and its handler."
            : $"The console host found more than one entry command: {names}. Keep one.");
    }
}
