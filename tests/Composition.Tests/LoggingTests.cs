namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umpire.Kernels;

public sealed class LoggingTests {
    [Test]
    public async Task Compose_gives_a_null_logger_when_no_host_gives_a_logger() {
        await using var provider = new TestApp().Compose(new ServiceCollection()).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<ILoggerFactory>(), Is.SameAs(NullLoggerFactory.Instance));
        Assert.That(provider.GetRequiredService<ILogger<LoggingTests>>().IsEnabled(LogLevel.Critical), Is.False);
    }

    [Test]
    public async Task A_logger_of_the_host_gets_the_logs_of_a_typed_logger() {
        using var logs = new RecordedLogs();

        await using var provider = new TestApp()
            .With(services => services.AddLogging(logging => logging.AddProvider(logs)))
            .Compose(new ServiceCollection()).BuildServiceProvider();
        provider.GetRequiredService<ILogger<LoggingTests>>().LogWarning("from the host logger");

        Assert.That(logs.Messages, Is.EqualTo(new[] { "from the host logger" }));
    }

    [Test]
    public async Task A_module_that_adds_logging_keeps_the_open_options_of_the_library() {
        await using var provider = new TestApp(new OptionsModule()).Compose(new ServiceCollection())
            .BuildServiceProvider();

        Assert.That(provider.GetRequiredService<OptionsClient>().Value, Is.EqualTo("set"));
        Assert.That(provider.GetRequiredService<ILoggerFactory>(), Is.TypeOf<LoggerFactory>());
    }

    [Test]
    public void A_log_provider_with_no_logger_factory_stops_the_compose() {
        var app = new TestApp().With(services => services.AddSingleton<ILoggerProvider>(new RecordedLogs()));

        var error = Assert.Throws<InvalidOperationException>(() => app.Compose(new ServiceCollection()));

        Assert.That(error.Message, Does.Contain("ILoggerProvider").And.Contain("AddLogging"));
    }

    public sealed class ProbeOptions {
        public string Value { get; set; } = "";
    }

    public sealed record OptionsClient(string Value);

    public sealed class OptionsModule : Module {
        protected override void Configure(IServiceCollection services) {
            services.AddLogging();
            services.Configure<ProbeOptions>(options => options.Value = "set");
            services.AddSingleton(provider =>
                new OptionsClient(provider.GetRequiredService<IOptions<ProbeOptions>>().Value.Value));
        }
    }

    sealed class RecordedLogs : ILoggerProvider, ILogger {
        readonly List<string> messages = [];

        public IReadOnlyList<string> Messages {
            get {
                lock (messages) {
                    return [.. messages];
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            lock (messages) {
                messages.Add(formatter(state, exception));
            }
        }

        public void Dispose() {
        }
    }
}
