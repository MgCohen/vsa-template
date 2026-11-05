using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace VSA.Analyzers;

// VSA1004: presenters do not publish events, and do not send events to the views (the observer, LivePresenters). The layer map gives presenters the event types, so that they can
// show them (IPresenterEvent). A change of state is a request, and its handler publishes the event.
public sealed class PresenterPublishRule() : ForbiddenUseRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1004",
        "Presenters do not publish events",
        "Presenter {0} uses {1}. A presenter does not publish events. Send a request with IBus, and publish the event in its handler.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("VSA.Presenters.Presenter");

    // The observer and the live presenters also send an event to the views, so a presenter does not use them.
    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("VSA.Contracts.Events.IIntegrationEventPublisher"),
        compilation.GetTypeByMetadataName("VSA.Contracts.Events.IIntegrationEventObserver"),
        compilation.GetTypeByMetadataName("VSA.Presenters.LivePresenters"),
    ];
}
