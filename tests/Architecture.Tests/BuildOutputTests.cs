namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class BuildOutputTests {
    [TestCase("Src/obj/Debug/X.cs")]
    [TestCase("Src/bin/X.cs")]
    [TestCase(@"Src\obj\X.cs")]
    public void A_file_in_an_obj_or_bin_folder_is_build_output(string path) {
        Assert.That(BuildOutput.Contains(path), Is.True);
    }

    [TestCase("Src/Objects/X.cs")]
    [TestCase("Src/binders/X.cs")]
    [TestCase("Src/obj")]
    public void A_file_outside_an_obj_or_bin_folder_is_not_build_output(string path) {
        Assert.That(BuildOutput.Contains(path), Is.False);
    }

    [Test]
    public void The_folder_name_ignores_case() {
        Assert.That(BuildOutput.Contains("Src/OBJ/X.cs"), Is.True);
    }
}
