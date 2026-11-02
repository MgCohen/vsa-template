using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Shelf;
using Shelf.Presenters;
using VSA.Host;

// This assembly records a layer that does not exist, for MissingModule below.
[assembly: AssemblyMetadata("VsaLayerAssembly", "Framework.Tests.Presenters")]

namespace Framework.Tests.Host;

public sealed class LayerLoadingTests {
    [Test]
    public void HostLoadsOnlyTheRecordedLayers() {
        // Shelf records its request and presenter layers. It has no Shelf.Handlers.Events, and that is not an error.
        var builder = WebApplication.CreateBuilder();

        builder.AddVsa(new ShelfModule());

        Assert.That(builder.Services.Any(s => s.ServiceType == typeof(ShelfPresenter)), Is.True);
    }

    [Test]
    public void RecordedLayerThatDoesNotLoadIsAnError() {
        var builder = WebApplication.CreateBuilder();

        var error = Assert.Throws<FileNotFoundException>(() => builder.AddVsa(new ShelfModule(), new MissingModule()));

        Assert.That(error.Message, Does.Contain("Framework.Tests.Presenters"));
    }

    sealed class MissingModule : VSA.Module.Module {
        public override void Configure(VSA.Module.ModuleBuilder module) {
        }
    }
}
