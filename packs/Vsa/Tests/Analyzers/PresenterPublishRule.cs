using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Umpire.Vsa.Analyzers;

// UMP1004: presenters do not publish events, and do not send events to the views (the observer, LivePresenters). The layer map gives presenters the event types, so that they can
// show them (IPresenterEvent). A change of state is a request, and its handler publishes the event.
public sealed class PresenterPublishRule() : ForbiddenUseRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "UMP1004",
        "Presenters do not publish events",
        "Presenter {0} uses {1}. A presenter does not publish events. Send a request with IBus, and publish the event in its handler.",
        "Umpire",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("Umpire.Vsa.Presenters.Presenter");

    // The observer and the live presenters also send an event to the views, so a presenter does not use them.
    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Events.IIntegrationEventPublisher"),
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Events.IIntegrationEventObserver"),
        compilation.GetTypeByMetadataName("Umpire.Vsa.Presenters.LivePresenters"),
    ];
}
