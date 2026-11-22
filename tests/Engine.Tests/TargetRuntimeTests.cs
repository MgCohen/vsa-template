using Blocks.Slices.FindBlocks;
using BuiltApp.Infrastructure;

namespace Engine.Tests;

public sealed class TargetRuntimeTests {
    static string Notes => Path.Combine(AppContext.BaseDirectory, "Notes.dll");

    [Test]
    public void A_net10_dll_gives_its_target_runtime() =>
        Assert.That(TargetRuntime.Of(Notes)?.Major, Is.EqualTo(10));

    [Test]
    public void A_bin_of_a_net10_app_is_newer_than_a_net8_engine() {
        var newer = TargetRuntime.NewerThan(AppContext.BaseDirectory, new Version(8, 0, 21));

        Assert.That(newer?.Runtime.Major, Is.EqualTo(10));
    }

    [Test]
    public void A_bin_of_a_net10_app_is_not_newer_than_a_net10_engine() =>
        Assert.That(TargetRuntime.NewerThan(AppContext.BaseDirectory, new Version(10, 0, 0)), Is.Null);

    [Test]
    public void A_file_that_is_not_a_dll_has_no_target_runtime() {
        var file = Path.Combine(AppContext.BaseDirectory, "Engine.Tests.runtimeconfig.json");

        Assert.That(TargetRuntime.Of(file), Is.Null);
    }

    [Test]
    public void The_finding_gives_the_runtime_and_the_fix() {
        var message = FindBlocksHandler.NewerRuntime(new NewerDll("C:/app/bin/Notes.dll", new Version(10, 0)));

        Assert.That(message, Does.Contain("Notes.dll targets .NET 10").And.Contain("Install .NET 10")
            .And.Contain("--roll-forward LatestMajor"));
    }
}
