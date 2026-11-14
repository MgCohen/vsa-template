using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace VSA.Analyzers;

// VSA1002: event handlers do not send requests. An event handler reacts to a fact. It does not use a request type
// or IBus. VSA.Sdk gives the Handlers/Events layer no reference to Contracts/Requests, so this rule catches the rest.
public sealed class EventHandlerRequestRule() : ForbiddenUseRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1002",
        "Event handlers do not send requests",
        "Event handler {0} uses {1}. An event handler does not send requests. Move the work to a request handler, or publish an event.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("VSA.Handlers.Events.IntegrationEventHandler`1");

    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("VSA.Contracts.Requests.IRoutedRequest"),
        compilation.GetTypeByMetadataName("VSA.Presenters.IBus"),
    ];
}
