namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

public sealed class EventHandlerRequestRule : ForbiddenUseRule {
    public override int Number => 2;

    protected override string Title => "Event handlers do not send requests";

    protected override string Message =>
        "Event handler {0} uses {1}. An event handler does not send requests. Move the work to a request handler, or publish an event.";

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("Umpire.Vsa.Handlers.Events.IntegrationEventHandler`1");

    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Requests.IRequest"),
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Requests.IRequestSender"),
    ];
}
