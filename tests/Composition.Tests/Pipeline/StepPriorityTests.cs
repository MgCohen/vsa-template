namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class StepPriorityTests {
    const int LockPriority = -10;

    [Test]
    public async Task A_step_with_a_priority_below_validation_runs_before_the_validators() {
        using var run = Services(new PipelineBuilder<Move>()
            .Handlers(typeof(MoveHandler))
            .BeforeStep<TagStep>()
            .BeforeStep<EnterLock>(LockPriority)
            .AfterStep<ExitLock>(LockPriority)
            .AfterStep<AuditStep>());

        var result = await Send(run, new Move(2));

        Assert.That(result.Value, Is.EqualTo(2));
        Assert.That(Log(run), Is.EqualTo(new[] { "enter", "validate", "tag", "handle", "audit", "exit" }));
    }

    [Test]
    public async Task Steps_with_one_priority_run_in_the_order_of_the_builder() {
        using var run = Services(new PipelineBuilder<Move>()
            .Handlers(typeof(MoveHandler))
            .BeforeStep<EnterLock>(LockPriority)
            .BeforeStep<FirstStep>(LockPriority)
            .AfterStep<AuditStep>(LockPriority)
            .AfterStep<ExitLock>(LockPriority));

        await Send(run, new Move(2));

        Assert.That(Log(run), Is.EqualTo(new[] { "enter", "first", "validate", "handle", "audit", "exit" }));
    }

    [Test]
    public async Task The_release_step_runs_after_a_validator_rejects() {
        using var run = LockedServices();

        var result = await Send(run, new Move(-1));

        Assert.That(result.Error?.Code, Is.EqualTo("move.negative"));
        Assert.That(Log(run), Is.EqualTo(new[] { "enter", "validate", "exit" }));
        Assert.That(run.Services.GetRequiredService<MatchLock>().IsFree, Is.True);
    }

    [Test]
    public void The_release_step_runs_after_the_handler_throws() {
        using var run = LockedServices();

        Assert.ThrowsAsync<InvalidOperationException>(() => Send(run, new Move(MoveHandler.Throws)));

        Assert.That(Log(run), Is.EqualTo(new[] { "enter", "validate", "handle", "exit" }));
        Assert.That(run.Services.GetRequiredService<MatchLock>().IsFree, Is.True);
    }

    [Test]
    public void The_release_step_runs_after_an_earlier_after_step_throws() {
        using var run = Services(new PipelineBuilder<Move>()
            .Handlers(typeof(MoveHandler))
            .BeforeStep<EnterLock>(LockPriority)
            .AfterStep<ExitLock>(LockPriority)
            .AfterStep<BrokenAfterStep>());

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => Send(run, new Move(2)));

        Assert.That(error!.Message, Is.EqualTo("broken after step"));
        Assert.That(Log(run), Is.EqualTo(new[] { "enter", "validate", "handle", "broken", "exit" }));
        Assert.That(run.Services.GetRequiredService<MatchLock>().IsFree, Is.True);
    }

    [Test]
    public void The_release_step_does_not_release_a_lock_that_the_run_did_not_take() {
        using var run = Services(new PipelineBuilder<Move>()
            .Handlers(typeof(MoveHandler))
            .BeforeStep<BrokenBeforeStep>(LockPriority - 1)
            .BeforeStep<EnterLock>(LockPriority)
            .AfterStep<ExitLock>(LockPriority));

        Assert.ThrowsAsync<InvalidOperationException>(() => Send(run, new Move(2)));

        Assert.That(Log(run), Is.EqualTo(new[] { "broken", "exit" }));
        Assert.That(run.Services.GetRequiredService<MatchLock>().IsFree, Is.True);
    }

    [Test]
    public async Task The_lock_step_makes_the_runs_of_one_match_serial_with_their_validators() {
        using var run = LockedServices();
        var slow = run.Services.GetRequiredService<SlowGate>();

        var first = Send(run, new Move(MoveHandler.Waits));
        await slow.Started.Task;
        var second = Send(run, new Move(3));
        await Task.Delay(50);
        var logBeforeRelease = Log(run);
        slow.Release();
        await Task.WhenAll(first, second);

        Assert.That(logBeforeRelease, Is.EqualTo(new[] { "enter", "validate", "handle" }));
        Assert.That(Log(run), Is.EqualTo(new[] {
            "enter", "validate", "handle", "exit", "enter", "validate", "handle", "exit",
        }));
    }

    [Test]
    public async Task A_registered_factory_gives_the_step_instance() {
        using var run = Services(
            new PipelineBuilder<Move>().Handlers(typeof(MoveHandler)).BeforeStep<NamedStep>(LockPriority),
            more => more.AddScoped(provider => new NamedStep(provider.GetRequiredService<StepLog>(), "factory")));

        await Send(run, new Move(2));

        Assert.That(Log(run), Is.EqualTo(new[] { "factory", "validate", "handle" }));
    }

    static StepRun LockedServices() => Services(new PipelineBuilder<Move>()
        .Handlers(typeof(MoveHandler))
        .BeforeStep<EnterLock>(LockPriority)
        .AfterStep<ExitLock>(LockPriority));

    static StepRun Services(PipelineBuilder<Move> builder, Action<IServiceCollection>? more = null) {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes(new FixedModules(), typeof(StepPriorityTests).Assembly));
        services.AddSingleton<StepLog>();
        services.AddSingleton<MatchLock>();
        services.AddSingleton<SlowGate>();
        more?.Invoke(services);
        var provider = services.BuildServiceProvider();
        return new StepRun(provider, builder.Build(provider));
    }

    static Task<Result<int>> Send(StepRun run, Move move) => run.Pipeline.SendAsync<int>(move);

    static IReadOnlyList<string> Log(StepRun run) => run.Services.GetRequiredService<StepLog>().Lines;

    public sealed class StepRun(ServiceProvider services, Pipeline pipeline) : IDisposable {
        public ServiceProvider Services => services;
        public Pipeline Pipeline => pipeline;

        public void Dispose() => services.Dispose();
    }

    public sealed record Move(int Value);

    public sealed class StepLog {
        readonly List<string> lines = [];

        public IReadOnlyList<string> Lines {
            get {
                lock (lines) {
                    return [.. lines];
                }
            }
        }

        public void Add(string line) {
            lock (lines) {
                lines.Add(line);
            }
        }
    }

    public sealed class MatchLock {
        readonly SemaphoreSlim gate = new(1, 1);

        public bool IsFree => gate.CurrentCount == 1;

        public Task EnterAsync() => gate.WaitAsync();

        public void Exit() => gate.Release();
    }

    public sealed class SlowGate {
        readonly SemaphoreSlim gate = new(0);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => gate.Release();

        public async Task WaitAsync() {
            Started.TrySetResult();
            await gate.WaitAsync();
        }
    }

    public sealed class MoveHandler(StepLog log, SlowGate slow) : IRunnableHandler<Move> {
        public const int Throws = 13;
        public const int Waits = 7;

        public async Task<IMessageResult> RunAsync(IEnvelope<Move> envelope) {
            log.Add("handle");
            if (envelope.Message.Value == Throws) {
                throw new InvalidOperationException("broken handler");
            }
            if (envelope.Message.Value == Waits) {
                await slow.WaitAsync();
            }
            return (Result<int>)envelope.Message.Value;
        }
    }

    public sealed class MoveValidator(StepLog log) : Validator<Move> {
        public override Error? Validate(IEnvelope<Move> envelope) {
            log.Add("validate");
            return envelope.Message.Value < 0 ? new Error("move.negative", "A move has no negative value.") : null;
        }
    }

    public sealed class EnterLock(MatchLock matchLock, StepLog log) : BeforeStep {
        public const string HeldKey = "match.lock";

        public override async Task<object> BeforeAsync(Envelope envelope) {
            await matchLock.EnterAsync();
            envelope.Items[HeldKey] = true;
            log.Add("enter");
            return envelope.Message;
        }
    }

    public sealed class ExitLock(MatchLock matchLock, StepLog log) : AfterStep {
        public override Task AfterAsync(Envelope envelope) {
            log.Add("exit");
            if (envelope.Items.Remove(EnterLock.HeldKey)) {
                matchLock.Exit();
            }
            return Task.CompletedTask;
        }
    }

    public sealed class TagStep(StepLog log) : BeforeStep<Move> {
        public override Task<Move> BeforeAsync(IEnvelope<Move> envelope) {
            log.Add("tag");
            return Task.FromResult(envelope.Message);
        }
    }

    public sealed class FirstStep(StepLog log) : BeforeStep {
        public override Task<object> BeforeAsync(Envelope envelope) {
            log.Add("first");
            return Task.FromResult(envelope.Message);
        }
    }

    public sealed class NamedStep(StepLog log, string name) : BeforeStep {
        public override Task<object> BeforeAsync(Envelope envelope) {
            log.Add(name);
            return Task.FromResult(envelope.Message);
        }
    }

    public sealed class AuditStep(StepLog log) : AfterStep {
        public override Task AfterAsync(Envelope envelope) {
            log.Add("audit");
            return Task.CompletedTask;
        }
    }

    public sealed class BrokenBeforeStep(StepLog log) : BeforeStep {
        public override Task<object> BeforeAsync(Envelope envelope) {
            log.Add("broken");
            throw new InvalidOperationException("broken before step");
        }
    }

    public sealed class BrokenAfterStep(StepLog log) : AfterStep {
        public override Task AfterAsync(Envelope envelope) {
            log.Add("broken");
            throw new InvalidOperationException("broken after step");
        }
    }
}
