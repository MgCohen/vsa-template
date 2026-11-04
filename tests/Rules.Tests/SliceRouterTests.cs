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
        sealed class CreateNoteHandler(IRepository<Note, NoteId> notes) : RoutedHandler<CreateNoteRequest, CreateNoteResponse> {
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
            File("ListNotes/ListNotesHandler.cs", "sealed class ListNotesHandler : RoutedHandler<ListNotesRequest, ListNotesResponse> { }"),
            File("Shared/NoteSummary.cs", "public sealed record NoteSummary(string Title);"),
            File("ShowNote/ShowNoteResponse.cs", "public sealed record ShowNoteResponse(NoteSummary Summary) : IResponse;"),
            File("ShowNote/ShowNoteHandler.cs", "sealed class ShowNoteHandler : RoutedHandler<ShowNoteRequest, ShowNoteResponse> { }"));

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

    // A Shared module gives base types. The router reads the Shared slice files as known files and follows the base names.
    static SliceSource Known(string path, string text) => new(Path.GetFullPath(Path.Combine(Slices, "..", "..", "Pipeline", "Slices", path)), text);

    static readonly SliceSource[] PipelineBases = [
        Known("Messages/Message.cs", "public abstract record Message; public sealed record MessageId(long Value) : IResponse;"),
        Known("Messages/MessageHandler.cs", "public abstract class MessageHandler<T> : RoutedHandler<T, MessageId> { }"),
        Known("Messages/MessageEvent.cs", "public abstract record MessageEvent : IntegrationEvent;"),
    ];

    [Test]
    public void A_type_gets_its_kind_through_a_Shared_base() {
        var routing = SliceRouter.Route(Slices, [
            File("DealDamage/DealDamage.cs", $"public sealed record DealDamage(int Amount) : Message, IRequest<MessageId> {{ {Routed} }}"),
            File("DealDamage/DealDamageHandler.cs", "sealed class DealDamageHandler : MessageHandler<DealDamage> { }"),
            File("DealDamage/DamageDealt.cs", "public sealed record DamageDealt : MessageEvent;"),
        ], PipelineBases, isShared: false);

        Assert.That(routing.Errors, Is.Empty);
        using var scope = Assert.EnterMultipleScope();
        Assert.That(LayerOf(routing, "DealDamage/DealDamage.cs"), Is.EqualTo("Contracts/Requests"));
        Assert.That(LayerOf(routing, "DealDamage/DealDamageHandler.cs"), Is.EqualTo("Handlers/Requests"));
        Assert.That(LayerOf(routing, "DealDamage/DamageDealt.cs"), Is.EqualTo("Contracts/Events"));
        Assert.That(routing.Layers.Keys, Has.None.Contains("Pipeline"), "The router does not route the known files.");
    }

    // Two levels: BigDamageHandler : DamageHandler (own slice) : MessageHandler (Shared) : RoutedHandler.
    [Test]
    public void The_router_follows_the_bases_to_any_depth() {
        var routing = SliceRouter.Route(Slices, [
            File("Damage/DamageHandler.cs", "abstract class DamageHandler : MessageHandler<Hit> { } sealed class BigDamageHandler : DamageHandler { }"),
        ], PipelineBases, isShared: false);

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

    // In a Shared module, Slices/Shared/ holds bases for the modules: a type that no contract uses goes to Contracts/Requests.
    [Test]
    public void In_a_Shared_module_an_unused_type_in_Shared_is_a_contracts_base() {
        var routing = SliceRouter.Route(Slices, [File("Shared/Id.cs", "public abstract record Id(long Value);")], [], isShared: true);

        Assert.That(routing.Errors, Is.Empty);
        Assert.That(LayerOf(routing, "Shared/Id.cs"), Is.EqualTo("Contracts/Requests"));
    }

    static IEnumerable<TestCaseData> Errors() {
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse,
            File("CreateNote/CreateNoteHandler.cs", "sealed class CreateNoteHandler : RoutedHandler<CreateNoteRequest, CreateNoteResponse> { } sealed class Log : IntegrationEventHandler<X> { }") },
            "has a request handler and an event handler").SetName("A slice with two kinds of handler");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse, File("CreateNote/TitleRules.cs", "static class TitleRules { }") },
            "has no handler").SetName("A slice with a helper and no handler");
        yield return new TestCaseData(new[] { CreateNoteResponse, CreateNoteHandler,
            File("CreateNote/CreateNoteRequest.cs", $"public sealed record CreateNoteRequest : IRequest<CreateNoteResponse> {{ {Routed} }} sealed class Helper : RoutedHandler<A, B> {{ }}") },
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
            File("ListNotes/ListNotesHandler.cs", "sealed class ListNotesHandler : RoutedHandler<A, B> { }") },
            "Move NoteDto to Slices/Shared/").SetName("A DTO from another slice");
        yield return new TestCaseData(new[] { CreateNoteRequest, CreateNoteResponse, CreateNoteHandler, File("Shared/TitleRules.cs", "static class TitleRules { }") },
            "Slices/Shared/ holds only contracts").SetName("A helper in Shared");
        yield return new TestCaseData(new[] { File("Loose.cs", "static class Loose { }") },
            "Move it into a slice folder").SetName("A file directly in Slices");
    }

    [TestCaseSource(nameof(Errors))]
    public void Routing_error(SliceSource[] files, string message) =>
        Assert.That(Route(files).Errors.Select(e => e.Message), Has.Some.Contains(message));
}
