namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

public sealed class EventHandlerRequestRule() : ForbiddenUseRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "UMP010202",
        "Event handlers do not send requests",
        "Event handler {0} uses {1}. An event handler does not send requests. Move the work to a request handler, or publish an event.",
        "Umpire",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("Umpire.Vsa.Handlers.Events.IntegrationEventHandler`1");

    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Requests.IRequest"),
        compilation.GetTypeByMetadataName("Umpire.Vsa.Presenters.IBus"),
    ];
}
