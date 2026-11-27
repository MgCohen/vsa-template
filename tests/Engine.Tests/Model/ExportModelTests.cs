namespace CheckModel.Tests;

using Engine.Tests;
using Microsoft.Extensions.DependencyInjection;
using Model.Slices.ExportModel;
using Umpire.Architecture;
using Umpire.CheckModel.Export;
using Umpire.Vsa.Contracts.Commands;

public sealed class ExportModelTests {
    [Test]
    public async Task The_export_operation_writes_the_definition_the_block_and_the_libraries() {
        using var temp = new TempState();
        var resolved = new ResolvedArchitecture(new EveryFormArchitecture());
        var library = LibraryExport.Of("Library", null, [new LibraryType("Library.Item", "Library/Item.cs", 3, 1, [])]);
        var command = new ExportModelCommand(temp.State, resolved, new BlockInput(temp.Block, ["Sample"]), [library]);

        using var host = new EngineTestApp(RunEngineTests.EngineModules()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);

        Assert.Multiple(() => {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(temp.State.ReadDefinition(), Is.EqualTo(DefinitionExport.Of(resolved).Write()));
            Assert.That(temp.State.ReadBlock().Projects, Is.EqualTo(new[] { "Sample" }));
            Assert.That(File.Exists(temp.State.LibraryFile("Library")), Is.True);
        });
    }
}
