namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

public sealed class PresenterPublishRule() : ForbiddenUseRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "UMP010204",
        "Presenters do not publish events",
        "Presenter {0} uses {1}. A presenter does not publish events. Send a request with IBus, and publish the event in its handler.",
        "Umpire",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("Umpire.Vsa.Presenters.Presenter");

    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Events.IIntegrationEventPublisher"),
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Events.IIntegrationEventObserver"),
        compilation.GetTypeByMetadataName("Umpire.Vsa.Presenters.LivePresenters"),
    ];
}
