namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class ValidatorBaseTests {
    [Test]
    public async Task A_pipeline_with_no_validator_base_runs_the_validators_of_Validator() {
        var result = await Send(new PipelineBuilder<Note>().CallsTestHandlers(), new Note("plain"));

        Assert.That(result.Error?.Code, Is.EqualTo("note.plain"));
    }

    [Test]
    public async Task A_pipeline_with_a_validator_base_runs_only_the_validators_of_that_base() {
        var plain = await Send(new PipelineBuilder<Note>().CallsTestHandlers().ValidatorsOf(typeof(NoteCheck<>)), new Note("plain"));
        var checkedNote = await Send(new PipelineBuilder<Note>().CallsTestHandlers().ValidatorsOf(typeof(NoteCheck<>)), new Note("check"));

        Assert.That(plain.IsSuccess, Is.True);
        Assert.That(checkedNote.Error?.Code, Is.EqualTo("note.check"));
    }

    [Test]
    public async Task A_validator_of_an_interface_of_the_message_runs_for_the_message() {
        var result = await Send(new PipelineBuilder<Note>().CallsTestHandlers().ValidatorsOf(typeof(NoteCheck<>)), new Note("titled"));

        Assert.That(result.Error?.Code, Is.EqualTo("note.titled"));
    }

    [Test]
    public async Task A_pipeline_runs_the_validators_of_each_of_its_bases() {
        var builder = new PipelineBuilder<Note>().CallsTestHandlers().ValidatorsOf(typeof(NoteCheck<>), typeof(Validator<>));

        var plain = await Send(builder, new Note("plain"));

        Assert.That(plain.Error?.Code, Is.EqualTo("note.plain"));
    }

    [TestCase(typeof(Validator<Note>))]
    [TestCase(typeof(BeforeStep<>))]
    [TestCase(typeof(string))]
    [TestCase(typeof(TwoMessageCheck<,>))]
    public void ValidatorsOf_rejects_a_type_that_is_not_a_validator_base(Type notABase) {
        var error = Assert.Throws<ArgumentException>(() => new PipelineBuilder<Note>().CallsTestHandlers().ValidatorsOf(notABase));

        Assert.That(error!.Message, Does.StartWith($"{notABase.FullName} is not a validator base."));
    }

    static async Task<Result<string>> Send(PipelineBuilder<Note> builder, Note note) {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes(new FixedModules(), typeof(ValidatorBaseTests).Assembly));
        using var provider = services.BuildServiceProvider();
        return await builder.Handlers(typeof(NoteHandler)).Build(provider).SendAsync<string>(note);
    }

    public interface ITitled {
        string Text { get; }
    }

    public sealed record Note(string Text) : ITitled;

    public sealed class NoteHandler : ITestHandler<Note> {
        public Task<IMessageResult> RunAsync(IEnvelope<Note> envelope) =>
            Task.FromResult<IMessageResult>((Result<string>)envelope.Message.Text);
    }

    [AutoRegister]
    public abstract class NoteCheck<TMessage> : IValidation where TMessage : class {
        public abstract Error? Check(IEnvelope<TMessage> envelope);

        Error? IValidation.Validate(Envelope envelope) => Check((IEnvelope<TMessage>)envelope);
    }

    public abstract class TwoMessageCheck<TFirst, TSecond> : IValidation {
        public Error? Validate(Envelope envelope) => null;
    }

    public sealed class PlainNoteValidator : Validator<Note> {
        public override Error? Validate(IEnvelope<Note> envelope) =>
            envelope.Message.Text == "plain" ? new Error("note.plain", "A plain note is not valid.") : null;
    }

    public sealed class CheckedNote : NoteCheck<Note> {
        public override Error? Check(IEnvelope<Note> envelope) =>
            envelope.Message.Text == "check" ? new Error("note.check", "A checked note is not valid.") : null;
    }

    public sealed class TitledCheck : NoteCheck<ITitled> {
        public override Error? Check(IEnvelope<ITitled> envelope) =>
            envelope.Message.Text == "titled" ? new Error("note.titled", "A titled note is not valid.") : null;
    }
}
