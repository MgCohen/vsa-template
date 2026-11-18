namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Definition;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Requests;

public sealed class ArchitectureTestBaseTests : ArchitectureTest<VsaArchitecture> {
    const string Slice = "Features/Notebook/Slices/Archive";

    [Test]
    public void A_command_handler_in_a_slice_binds_to_the_command_handlers() {
        var place = At<SampleCommandHandler>(Slice);

        Assert.That(place.Part, Is.EqualTo("Handlers.Commands"));
        Assert.That(place.Values["Feature"], Is.EqualTo("Notebook"));
        Assert.That(place.Values["Slice"], Is.EqualTo("Archive"));
        Assert.That(place.Path, Is.EqualTo(Slice));
    }

    [Test]
    public void A_hook_in_a_slice_binds_to_the_command_hooks() {
        Assert.That(At<SampleHook>(Slice).Part, Is.EqualTo("Handlers.Commands.Hooks"));
    }

    [Test]
    public void A_helper_follows_its_user_when_the_user_is_a_neighbor() {
        Assert.That(At<SampleDetail>(Slice, typeof(SampleRequest)).Part, Is.EqualTo("Contracts.Requests"));
    }

    [Test]
    public void A_helper_with_no_neighbor_binds_to_its_folder() {
        Assert.That(At<SampleDetail>(Slice).Part, Is.EqualTo("Slice"));
    }

    [Test]
    public void A_plain_type_in_the_infrastructure_folder_binds_to_the_infrastructure() {
        Assert.That(At("Features/Notebook/Infrastructure").Part, Is.EqualTo("Infrastructure"));
    }

    [Test]
    public void A_type_in_a_folder_that_the_definition_does_not_have_is_not_mapped() {
        var place = At("Features/Notebook/Contracts");

        Assert.That(place.IsMapped, Is.False);
        Assert.That(place.Part, Is.EqualTo("None"));
    }

    [Test]
    public void A_type_in_a_pack_folder_binds_to_the_pack() {
        Assert.That(At("Packs/Ecs/Kernel").Part, Is.EqualTo("Pack"));
    }

    [Test]
    public void A_request_handler_does_not_use_a_request_of_another_slice() {
        var handler = At<SampleRequestHandler>(Slice);
        var request = At<SampleRequest>("Features/Notebook/Slices/GetNote");

        Assert.That(MayUse(handler, request).Permission, Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_command_handler_uses_a_command_of_another_slice() {
        var handler = At<SampleCommandHandler>(Slice);
        var command = At<SampleCommand>("Features/Notebook/Slices/GetNote");

        Assert.That(MayUse(handler, command).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_validator_in_a_slice_binds_to_the_validators() {
        Assert.That(At<SampleValidator>(Slice).Part, Is.EqualTo("Slice.Validators"));
    }

    [Test]
    public void A_validator_uses_the_request_of_its_slice() {
        var validator = At<SampleValidator>(Slice);
        var request = At<SampleRequest>(Slice);

        Assert.That(MayUse(validator, request).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_validator_does_not_use_a_request_of_another_slice() {
        var validator = At<SampleValidator>(Slice);
        var request = At<SampleRequest>("Features/Notebook/Slices/GetNote");

        Assert.That(MayUse(validator, request).Permission, Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_validator_does_not_use_the_infrastructure() {
        var validator = At<SampleValidator>(Slice);

        Assert.That(MayUse(validator, At("Features/Notebook/Infrastructure")).Permission, Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_validator_uses_a_base_command_of_a_plugin() {
        var validator = At<SampleValidator>(Slice);
        var baseCommand = At<SampleCommand>("Plugins/Journal/Contracts/Commands");

        Assert.That(baseCommand.Part, Is.EqualTo("Contracts.Commands"));
        Assert.That(MayUse(validator, baseCommand).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_validator_uses_a_request_handler_of_a_plugin() {
        var validator = At<SampleValidator>(Slice);
        var handler = At<SampleRequestHandler>("Plugins/Journal/Handlers/Requests");

        Assert.That(MayUse(validator, handler).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_subclass_with_no_user_follows_its_base_class() {
        var place = At<SampleCheck>(Slice, typeof(SampleCheckBase), typeof(SampleCheckingHandler));

        Assert.That(place.Part, Is.EqualTo("Handlers.Requests"));
    }

    [Test]
    public void A_subclass_with_no_base_class_in_its_folder_binds_to_its_folder() {
        Assert.That(At<SampleCheck>(Slice).Part, Is.EqualTo("Slice"));
    }

    [Test]
    public void A_use_of_a_place_that_is_not_mapped_throws() {
        var notMapped = At("Features/Notebook/Contracts");

        Assert.Throws<InvalidOperationException>(() => MayUse(notMapped, At("Features/Notebook/Domain")));
    }

    public sealed record SampleRequest : IRequest<SampleResponse> {
        public static Effect Effect => Effect.Read;
        public SampleDetail? Detail { get; init; }
    }

    public sealed record SampleResponse : IResponse;

    public sealed record SampleDetail(string Text);

    public sealed record SampleCommand : Umpire.Vsa.Contracts.Commands.ICommand<SampleResponse>;

    public sealed class SampleRequestHandler : RequestHandler<SampleRequest, SampleResponse> {
        public override Task<Umpire.Messages.Contracts.Result<SampleResponse>> HandleAsync(
            Umpire.Messages.Contracts.Envelope<SampleRequest> envelope) => throw new NotSupportedException();
    }

    public sealed class SampleCommandHandler : CommandHandler<SampleCommand, SampleResponse> {
        public override Task<Umpire.Messages.Contracts.Result<SampleResponse>> HandleAsync(
            Umpire.Messages.Contracts.Envelope<SampleCommand> envelope) => throw new NotSupportedException();
    }

    public sealed class SampleValidator : Umpire.Messages.Handlers.Validator<SampleRequest> {
        public override Umpire.Messages.Contracts.Error? Validate(Umpire.Messages.Contracts.IEnvelope<SampleRequest> envelope) => null;
    }

    public abstract class SampleCheckBase {
        public abstract bool Passes(SampleRequest request);
    }

    public sealed class SampleCheck : SampleCheckBase {
        public override bool Passes(SampleRequest request) => true;
    }

    public sealed class SampleCheckingHandler(SampleCheckBase check) : RequestHandler<SampleRequest, SampleResponse> {
        public override Task<Umpire.Messages.Contracts.Result<SampleResponse>> HandleAsync(
            Umpire.Messages.Contracts.Envelope<SampleRequest> envelope) =>
            check.Passes(envelope.Message) ? throw new NotSupportedException() : throw new InvalidOperationException();
    }

    public sealed class SampleHook : BeforeCommandHook<SampleCommand> {
        public override Task<SampleCommand> BeforeAsync(Umpire.Messages.Contracts.IEnvelope<SampleCommand> envelope) =>
            Task.FromResult(envelope.Message);
    }
}
