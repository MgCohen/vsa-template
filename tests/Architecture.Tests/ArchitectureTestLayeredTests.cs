namespace EngineCore.Tests;

extern alias testing;

using Umpire.Architecture;
using testing::Umpire.Architecture;

public sealed class ArchitectureTestLayeredTests : ArchitectureTest<LayeredArchitecture> {
    [TestCase("Ui", "Logic", Permission.Allow)]
    [TestCase("Data", "Ui", Permission.Deny)]
    public void The_base_reads_a_definition_with_no_VSA_type(string from, string to, Permission expected) {
        Assert.That(MayUse(At(from), At(to)).Permission, Is.EqualTo(expected));
    }
}
