using System.Reflection;
using Shelf;
using Shelf.Presenters;

// This assembly records a layer that does not exist, for MissingModule below.
[assembly: AssemblyMetadata("VsaLayerAssembly", "Framework.Tests.Presenters")]

namespace Framework.Tests.Host;

public sealed class LayerLoadingTests {
    [Test]
    public void HostLoadsOnlyTheRecordedLayers() {
        // Shelf records only its Presenters layer. It has no Shelf.Handlers.Requests or Shelf.Handlers.Events, and that is not an error.
        var registrations = WebApps.OnTestServer(new ShelfModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType == typeof(ShelfPresenter)), Is.True);
    }

    [Test]
    public void RecordedLayerThatDoesNotLoadIsAnError() {
        var builder = WebApps.OnTestServer(new ShelfModule(), new MissingModule());

        var error = Assert.Throws<FileNotFoundException>(() => builder.Build());

        Assert.That(error.Message, Does.Contain("Framework.Tests.Presenters"));
    }

    sealed class MissingModule : VSA.Module.Module {
        public override void Configure(VSA.Module.ModuleBuilder module) {
        }
    }
}
