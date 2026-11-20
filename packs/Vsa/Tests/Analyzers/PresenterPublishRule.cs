namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

public sealed class PresenterPublishRule : ForbiddenUseRule {
    public override int Number => 4;

    protected override string Title => "Presenters do not publish events";

    protected override string Message =>
        "Presenter {0} uses {1}. A presenter does not publish events. Send a request with IRequestSender, and publish the event in its handler.";

    protected override INamedTypeSymbol? Subject(Compilation compilation) =>
        compilation.GetTypeByMetadataName("Umpire.Vsa.Presenters.Presenter");

    protected override IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation) => [
        compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Events.IIntegrationEventPublisher"),
        compilation.GetTypeByMetadataName("Umpire.Messages.Handlers.Pipelines"),
    ];
}
