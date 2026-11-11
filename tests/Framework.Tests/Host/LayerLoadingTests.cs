using Shelf;
using Shelf.Presenters;

namespace Framework.Tests.Host;

public sealed class LayerLoadingTests {
    [Test]
    public void HostLoadsOnlyTheLayersThatExist() {
        // Shelf has only its Presenters layer. It has no Shelf.Handlers.Requests or Shelf.Handlers.Events, and that is not an error.
        var registrations = WebApps.OnTestServer(new ShelfModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType == typeof(ShelfPresenter)), Is.True);
    }
}
