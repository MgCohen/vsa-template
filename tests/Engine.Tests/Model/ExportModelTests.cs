namespace CheckModel.Tests;

using Blocks.Domain.Shared;
using Engine.Tests;
using Facts.Domain.Shared;
using LoadApp.Domain.Shared;
using Microsoft.Extensions.DependencyInjection;
using Model.Slices.ExportModel;
using Operations.Domain.Shared;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;
using Umpire.Vsa.Contracts.Commands;

public sealed class ExportModelTests {
    [Test]
    public async Task The_export_operation_writes_the_definition_the_block_the_libraries_and_the_pointers() {
        using var temp = new TempState();
        var own = Saved(temp, "Sample", Snapshot.Of("Sample", [Samples.File("Ui/Page.cs", Samples.Type("Ui.Page"))]) with {
            References = [new ProjectReference("Library", "")],
        });
        var library = Saved(temp, "Library", Snapshot.Of("Library", [Samples.File("Item.cs", Samples.Type("Item"))]));
        var list = Path.Combine(temp.Block, "snapshots.txt");
        File.WriteAllLines(list, [SnapshotList.Line(own), SnapshotList.Line(library)]);
        var dll = Path.Combine(temp.Block, "Library.dll");
        File.WriteAllText(dll, "");
        var app = new AppInput(new EveryFormArchitecture(), Tree(temp.Block), [dll], new Dictionary<string, Umpire.Architecture.Source>()) {
            Projects = ["Sample"],
        };

        var result = await Send(new ExportModelCommand(app, new BinIndex(temp.Block, [], []), temp.State.Folder, list, ReportMode.Error));

        Assert.Multiple(() => {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(temp.State.HasDefinition, Is.True);
            Assert.That(temp.State.ReadBlock().Projects, Is.EqualTo(new[] { "Sample" }));
            Assert.That(temp.State.ReadBlock().Mode, Is.EqualTo(CheckMode.Error));
            Assert.That(temp.State.ReadBlock().SnapshotOf("Sample", null), Is.Not.Null);
            Assert.That(File.Exists(temp.State.LibraryFile("Library")), Is.True);
            Assert.That(StatePointer.Read(own.File)?.Folder, Is.EqualTo(temp.State.Folder));
            Assert.That(Snapshot.Read(File.ReadAllText(own.File)).References[0].Surface, Is.Empty);
        });
    }

    [Test]
    public async Task The_export_writes_the_surface_of_a_project_of_the_block_and_removes_the_snapshot_of_an_old_target() {
        using var temp = new TempState();
        var used = Saved(temp, "Used", Snapshot.Of("Used", [Samples.File("Used.cs", Samples.Type("Used"))]) with { Target = "net8.0" });
        var user = Saved(temp, "Sample", Snapshot.Of("Sample", [Samples.File("Ui/Page.cs", Samples.Type("Ui.Page"))]) with {
            References = [new ProjectReference("Used", "")], Target = "net8.0",
        });
        var old = Saved(temp, "Sample-old", Snapshot.Of("Sample", []) with { Target = "net6.0" }) with { Project = "Sample" };
        var list = Path.Combine(temp.Block, "snapshots.txt");
        var app = new AppInput(new EveryFormArchitecture(), Tree(temp.Block), [], new Dictionary<string, Umpire.Architecture.Source>()) {
            Projects = ["Sample", "Used"],
        };
        File.WriteAllLines(list, [SnapshotList.Line(user), SnapshotList.Line(used), SnapshotList.Line(old)]);
        await Send(new ExportModelCommand(app, new BinIndex(temp.Block, [], []), temp.State.Folder, list, ReportMode.Warn));
        File.WriteAllLines(list, [SnapshotList.Line(user), SnapshotList.Line(used)]);

        await Send(new ExportModelCommand(app, new BinIndex(temp.Block, [], []), temp.State.Folder, list, ReportMode.Warn));

        Assert.Multiple(() => {
            Assert.That(Snapshot.Read(File.ReadAllText(user.File)).References[0].Surface,
                Is.EqualTo(Surface.Of(Snapshot.Read(File.ReadAllText(used.File)))));
            Assert.That(File.Exists(old.File), Is.False);
            Assert.That(temp.State.ReadBlock().Snapshots.Select(s => s.Target), Is.EqualTo(new[] { "net8.0", "net8.0" }));
        });
    }

    static SnapshotPlace Saved(TempState temp, string folder, Snapshot snapshot) {
        var file = Path.Combine(temp.Block, "obj", folder, "umpire", "snapshot.json");
        StateFolder.SaveSnapshot(file, snapshot with { Root = temp.Block });
        return new SnapshotPlace(snapshot.Project, file, snapshot.Target);
    }

    static BlockTree Tree(string block) => new(new BlockEntry(block, EntryForm.App, null, block), []);

    static async Task<Umpire.Messages.Contracts.Result<ExportModelResult>> Send(ExportModelCommand command) {
        using var host = new EngineTestApp(RunEngineTests.EngineModules()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);
    }
}
