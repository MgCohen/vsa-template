using VSA.Sdk.Tasks;

namespace Rules.Tests;

// The slice router of VSA.Sdk (framework/Sdk/Tasks/SliceRouter.cs). It reads the files of Slices/ and selects
// the project of each file by its types. The files are in memory: the router reads only the paths and the text.
public sealed class SliceRouterTests {
    static readonly string Slices = Path.Combine(Path.GetTempPath(), "vsa-router", "Slices");

    const string Routed = "public static HttpMethod Method => HttpMethod.Post; public static string Route => \"/notes\";";

    // The router keys each file by its full path. On Windows, the full path has '\' in place of '/'.
    static string At(string path) => Path.GetFullPath(Path.Combine(Slices, path));

    static SliceSource File(string path, string text) => new(At(path), text);

    static SliceRouting Route(params SliceSource[] files) => SliceRouter.Route(Slices, files);

    static string LayerOf(SliceRouting routing, string path) =>
        routing.Layers.TryGetValue(At(path), out var layer) ? layer : "(none)";

    static SliceSource CreateNoteRequest => File("CreateNote/CreateNoteRequest.cs",
        $"public sealed record CreateNoteRequest(string Title) : IRequest<CreateNoteResponse> {{ {Routed} }}");
    static SliceSource CreateNoteResponse => File("CreateNote/CreateNoteResponse.cs",
        "public sealed record CreateNoteResponse(Guid Id) : IResponse;");
    static SliceSource CreateNoteHandler => File("CreateNote/CreateNoteHandler.cs", """
        sealed class CreateNoteHandler(IRepository<Note, NoteId> notes) : RequestHandler<CreateNoteRequest, CreateNoteResponse> {
            protected override Task<Result<CreateNoteResponse>> HandleRequestAsync(CreateNoteRequest request, CancellationToken ct) => null!;
        }
        """);

    [Test]
    public void Each_file_goes_to_the_project_of_its_type() {
        var routing = Route(
            CreateNoteRequest, CreateNoteResponse, CreateNoteHandler,
            File("CreateNote/NoteCreated.cs", "public sealed record NoteCreated(Guid NoteId) : IntegrationEvent;"),
            File("CreateNote/TitleRules.cs", "static class TitleRules { }"),
            File("LogNoteCreated/NoteCreatedHandler.cs", "sealed class NoteCreatedHandler : IntegrationEventHandler<NoteCreated> { }"));

        Assert.That(routing.Errors, Is.Empty);
        using var scope = Assert.EnterMultipleScope();
        Assert.That(LayerOf(routing, "CreateNote/CreateNoteRequest.cs"), Is.EqualTo("Contracts/Requests"));
        Assert.That(LayerOf(routing, "CreateNote/CreateNoteResponse.cs"), Is.EqualTo("Contracts/Requests"));
        Assert.That(LayerOf(routing, "CreateNote/NoteCreated.cs"), Is.EqualTo("Contracts/Events"));
        Assert.That(LayerOf(routing, "CreateNote/CreateNoteHandler.cs"), Is.EqualTo("Handlers/Requests"));
        Assert.That(LayerOf(routing, "CreateNote/TitleRules.cs"), Is.EqualTo("Handlers/Requests"));
        Assert.That(LayerOf(routing, "LogNoteCreated/NoteCreatedHandler.cs"), Is.EqualTo("Handlers/Events"));
    }

    // NoteDto has no marker. The response uses it, so it goes with the response, to any depth.
    [Test]
    public void A_type_that_a_contract_uses_goes_with_the_contract() {
        var routing = Route(
            File("ListNotes/ListNotesRequest.cs", $"public sealed record ListNotesRequest : IRequest<ListNotesResponse> {{ {Routed} }}"),
            File("ListNotes/ListNotesResponse.cs", "public sealed record ListNotesResponse(IReadOnlyList<NoteDto> Notes) : IResponse;"),
            File("ListNotes/NoteDto.cs", "public sealed record NoteDto(Guid Id, AuthorDto Author);"),
            File("ListNotes/AuthorDto.cs", "public sealed record AuthorDto(string Name);"),
            File("ListNotes/ListNotesHandler.cs", "sealed class ListNotesHandler : RequestHandler<ListNotesRequest, ListNotesResponse> { }"),
            File("Shared/NoteSummary.cs", "public sealed record NoteSummary(string Title);"),
            File("ShowNote/ShowNoteResponse.cs", "public sealed record ShowNoteResponse(NoteSummary Summary) : IResponse;"),
            File("ShowNote/ShowNoteHandler.cs", "sealed class ShowNoteHandler : RequestHandler<ShowNoteRequest, ShowNoteResponse> { }"));

        Assert.That(routing.Errors, Is.Empty);
        using var scope = Assert.EnterMultipleScope();
        Assert.That(LayerOf(routing, "ListNotes/NoteDto.cs"), Is.EqualTo("Contracts/Requests"));
        Assert.That(LayerOf(routing, "ListNotes/AuthorDto.cs"), Is.EqualTo("Contracts/Requests"));
        Assert.That(LayerOf(routing, "Shared/NoteSummary.cs"), Is.EqualTo("Contracts/Requests"));
    }

    // A comment or a string can contain code. The scanner ignores them.
    [Test]
    public void Comments_and_strings_do_not_declare_types() {
        var routing = Route(CreateNoteRequest, CreateNoteResponse, CreateNoteHandler,
            File("CreateNote/Notes.cs", """
                // public sealed record Fake : IResponse;
                static class Notes { const string Text = "class Fake : IRequest<X>"; /* record Other : IntegrationEvent */ }
                """));

        Assert.That(routing.Errors, Is.Empty);
        Assert.That(LayerOf(routing, "CreateNote/Notes.cs"), Is.EqualTo("Handlers/Requests"));
    }

    // A plugin module gives base types. The router reads the layer folders of the plugin modules as known files
    // and follows the base names.
    static SliceSource Known(string path, string text) => new(Path.GetFullPath(Path.Combine(Slices, "..", "..", "Pipeline", path)), text);

    static readonly SliceSource[] PipelineBases = [
        Known("Contracts/Requests/Message.cs", "public abstract record Message; public sealed record MessageId(long Value) : IResponse;"),
        Known("Handlers/Requests/MessageHandler.cs", "public abstract class MessageHandler<T> : RequestHandler<T, MessageId> { }"),
        Known("Contracts/Events/MessageEvent.cs", "public abstract record MessageEvent : IntegrationEvent;"),
    ];

    [Test]
    public void A_type_gets_its_kind_through_a_plugin_base() {
        var routing = SliceRouter.Route(Slices, [
            File("DealDamage/DealDamage.cs", $"public sealed record DealDamage(int Amount) : Message, IRequest<MessageId> {{ {Routed} }}"),
            File("DealDamage/DealDamageHandler.cs", "sealed class DealDamageHandler : MessageHandler<DealDamage> { }"),
            File("DealDamage/DamageDealt.cs", "public sealed record DamageDealt : MessageEvent;"),
        ], PipelineBases);

        Assert.That(routing.Errors, Is.Empty);
        using var scope = Assert.EnterMultipleScope();
        Assert.That(LayerOf(routing, "DealDamage/DealDamage.cs"), Is.EqualTo("Contracts/Requests"));
        Assert.That(LayerOf(routing, "DealDamage/DealDamageHandler.cs"), Is.EqualTo("Handlers/Requests"));
        Assert.That(LayerOf(routing, "DealDamage/DamageDealt.cs"), Is.EqualTo("Contracts/Events"));
        Assert.That(routing.Layers.Keys, Has.None.Contains("Pipeline"), "The router does not route the known files.");
    }

    // Two levels: BigDamageHandler : DamageHandler (own slice) : MessageHandler (plugin) : RequestHandler.
    [Test]
    public void The_router_follows_the_bases_to_any_depth() {
        var routing = SliceRouter.Route(Slices, [
            File("Damage/DamageHandler.cs", "abstract class DamageHandler : MessageHandler<Hit> { } sealed class BigDamageHandler : DamageHandler { }"),
        ], PipelineBases);

        Assert.That(routing.Errors, Is.Empty);
        Assert.That(LayerOf(routing, "Damage/DamageHandler.cs"), Is.EqualTo("Handlers/Requests"));
    }

    [Test]
    public void A_loop_in_the_bases_does_not_stop_the_router() {
        var routing = Route(CreateNoteRequest, CreateNoteResponse, CreateNoteHandler,
            File("CreateNote/Loop.cs", "class A : B { } class B : A { }"));

        Assert.That(routing.Errors, Is.Empty);
        Assert.That(LayerOf(routing, "CreateNote/Loop.cs"), Is.EqualTo("Handlers/Requests"));
    }

    // A command and the result that it names go to Contracts/Commands. The other files of the slice go to Handlers/Commands.
    [Test]
    public void A_command_slice_goes_to_the_command_projects() {
        var routing = Route(
            File("PayCost/PayCost.cs", "public sealed record PayCost(Guid Payer, int Amount) : ICommand<PayCostResult>;"),
            File("PayCost/PayCostResult.cs", "public sealed record PayCostResult(bool Paid);"),
            File("PayCost/PayCostHandler.cs", "sealed class PayCostHandler : CommandHandler<PayCost, PayCostResult> { }"),
            File("PayCost/CostRules.cs", "static class CostRules { }"));

        Assert.That(routing.Errors, Is.Empty);
        using var scope = Assert.EnterMultipleScope();
        Assert.That(LayerOf(routing, "PayCost/PayCost.cs"), Is.EqualTo("Contracts/Commands"));
        Assert.That(LayerOf(routing, "PayCost/PayCostResult.cs"), Is.EqualTo("Contracts/Commands"));
        Assert.That(LayerOf(routing, "PayCost/PayCostHandler.cs"), Is.EqualTo("Handlers/Commands"));
        Assert.That(LayerOf(routing, "PayCost/CostRules.cs"), Is.EqualTo("Handlers/Commands"));
    }

    // A command can inherit the marker through a base of a plugin module, for example PayCost : Effect<T> : ICommand<T>.
    [Test]
    public void A_command_with_a_plugin_base_goes_to_Contracts_Commands() {
        var routing = SliceRouter.Route(Slices, [
            File("PayCost/PayCost.cs", "public sealed record PayCost(int Amount) : Effect<PayCostResult>;"),
            File("PayCost/PayCostResult.cs", "public sealed record PayCostResult(bool Paid);"),
            File("PayCost/PayCostHandler.cs", "sealed class PayCostHandler : EffectHandler<PayCost, PayCostResult> { }"),
        ], [
            new SliceSource(Path.Combine(Path.GetTempPath(), "vsa-router", "Effects", "Contracts", "Commands", "Effect.cs"),
                "public abstract record Effect<T> : ICommand<T>;"),
            new SliceSource(Path.Combine(Path.GetTempPath(), "vsa-router", "Effects", "Handlers", "Commands", "EffectHandler.cs"),
                "public abstract class EffectHandler<TC, TR> : CommandHandler<TC, TR> { }"),
        ]);

        Assert.That(routing.Errors, Is.Empty);
        using var scope = Assert.EnterMultipleScope();
        Assert.That(LayerOf(routing, "PayCost/PayCost.cs"), Is.EqualTo("Contracts/Commands"));
        Assert.That(LayerOf(routing, "PayCost/PayCostHandler.cs"), Is.EqualTo("Handlers/Commands"));
    }

    static IEnumerable<TestCaseData> Errors() {
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse,
            File("CreateNote/CreateNoteHandler.cs", "sealed class CreateNoteHandler : RequestHandler<CreateNoteRequest, CreateNoteResponse> { } sealed class Log : IntegrationEventHandler<X> { }") },
            "has a request handler and an event handler").SetName("A slice with two kinds of handler");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse, File("CreateNote/TitleRules.cs", "static class TitleRules { }") },
            "has no handler").SetName("A slice with a helper and no handler");
        yield return new TestCaseData(new[] { CreateNoteResponse, CreateNoteHandler,
            File("CreateNote/CreateNoteRequest.cs", $"public sealed record CreateNoteRequest : IRequest<CreateNoteResponse> {{ {Routed} }} sealed class Helper : RequestHandler<A, B> {{ }}") },
            "Put each in its own file").SetName("A file with a contract and a handler");
        yield return new TestCaseData(new[] { CreateNoteHandler,
            File("CreateNote/CreateNoteRequest.cs", $"public sealed record CreateNoteRequest : IRequest<CreateNoteResponse> {{ {Routed} }} public sealed record NoteCreated : IntegrationEvent;"),
            CreateNoteResponse },
            "Put each in its own file").SetName("A file with a request and an event");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteHandler,
            File("CreateNote/CreateNoteResponse.cs", "public sealed record CreateNoteResponse(NoteDto Note) : IResponse;"),
            File("CreateNote/NoteCreated.cs", "public sealed record NoteCreated(NoteDto Note) : IntegrationEvent;"),
            File("CreateNote/NoteDto.cs", "public sealed record NoteDto(string Title);") },
            "A type can be in one contracts project only").SetName("A DTO that a response and an event use");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteHandler,
            File("CreateNote/CreateNoteResponse.cs", "public sealed record CreateNoteResponse(NoteDto Note) : IResponse;"),
            File("ListNotes/NoteDto.cs", "public sealed record NoteDto(string Title);"),
            File("ListNotes/ListNotesHandler.cs", "sealed class ListNotesHandler : RequestHandler<A, B> { }") },
            "Move NoteDto to Slices/Shared/").SetName("A DTO from another slice");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse, CreateNoteHandler, File("Shared/TitleRules.cs", "static class TitleRules { }") },
            "Slices/Shared/ holds only contracts").SetName("A helper in Shared");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse,
            File("CreateNote/CreateNoteHandler.cs", "sealed class CreateNoteHandler : RequestHandler<CreateNoteRequest, CreateNoteResponse> { } sealed class Pay : CommandHandler<A, B> { }") },
            "has a request handler and a command handler").SetName("A slice with a request handler and a command handler");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteHandler,
            File("CreateNote/CreateNoteResponse.cs", "public sealed record CreateNoteResponse(NoteDto Note) : IResponse;"),
            File("CreateNote/SaveNote.cs", "public sealed record SaveNote(NoteDto Note) : ICommand<bool>;"),
            File("CreateNote/NoteDto.cs", "public sealed record NoteDto(string Title);") },
            "A type can be in one contracts project only").SetName("A DTO that a response and a command use");
        yield return new TestCaseData(new[] { File("Loose.cs", "static class Loose { }") },
            "Move it into a slice folder").SetName("A file directly in Slices");
    }

    [TestCaseSource(nameof(Errors))]
    public void Routing_error(SliceSource[] files, string message) =>
        Assert.That(Route(files).Errors.Select(e => e.Message), Has.Some.Contains(message));
}
