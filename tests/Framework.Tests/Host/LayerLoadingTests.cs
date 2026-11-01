using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Shelf;
using Shelf.Presenters;
using VSA.Host;

// This assembly records a layer that does not exist, for MissingModule below.
[assembly: AssemblyMetadata("VsaLayer", "Framework.Tests.Presenters")]

namespace Framework.Tests.Host;

public sealed class LayerLoadingTests {
    [Fact]
    public void HostLoadsOnlyTheRecordedLayers() {
        // Shelf records its request and presenter layers. It has no Shelf.Handlers.Events, and that is not an error.
        var builder = WebApplication.CreateBuilder();

        builder.AddVsa(new ShelfModule());

        Assert.Contains(builder.Services, s => s.ServiceType == typeof(ShelfPresenter));
    }

    [Fact]
    public void RecordedLayerThatDoesNotLoadIsAnError() {
        var builder = WebApplication.CreateBuilder();

        var error = Assert.Throws<FileNotFoundException>(() => builder.AddVsa(new ShelfModule(), new MissingModule()));

        Assert.Contains("Framework.Tests.Presenters", error.Message);
    }

    sealed class MissingModule : VSA.Module.Module {
        public override void Configure(VSA.Module.ModuleBuilder module) {
        }
    }
}
