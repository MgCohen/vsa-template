using Tests.Shared;

namespace Engine.Tests;

using BuiltApp.Infrastructure;

public sealed class DefinitionFolderTests {
    string block = null!;

    [SetUp]
    public void Create() => block = TempFolder.Create();

    [TearDown]
    public void Delete() => TempFolder.Delete(block);

    [TestCase("App/Packs/X/Definition/XArchitecture.cs", "App/Packs/X/Definition")]
    [TestCase("Tools/definition/Parts/ToolsArchitecture.cs", "Tools/definition")]
    public void A_definition_folder_below_the_block_with_source_is_found(string file, string folder) {
        Write(file);

        Assert.That(DefinitionFolder.Below(block, []), Is.EqualTo(new[] { Path.Combine(block, folder.Replace('/', Path.DirectorySeparatorChar)) }));
    }

    [TestCase("Definition/ShopArchitecture.cs")]
    [TestCase("App/Packs/X/Definition/X.Definition.csproj")]
    [TestCase("App/Packs/X/Definition/Tests/XTests.cs")]
    [TestCase("App/Tests/Definition/Stub.cs")]
    [TestCase("node_modules/a/Definition/Copy.cs")]
    [TestCase("artifacts~/Definition/Copy.cs")]
    [TestCase("Draft/Definition/Draft.cs")]
    public void Another_folder_is_not_found(string file) {
        Write(file);

        Assert.That(DefinitionFolder.Below(block, ["Draft"]), Is.Empty);
    }

    [Test]
    public void A_test_project_has_no_definition() {
        Write("Shop.Tests/Shop.Tests.csproj");
        Write("Shop.Tests/Definition/ShopTests.cs");

        Assert.That(DefinitionFolder.Below(block, []), Is.Empty);
    }

    [Test]
    public void A_folder_alias_of_the_definition_folder_is_not_below_the_block() {
        Write("Assets/Shop/Definition/ShopArchitecture.cs");
        File.WriteAllText(Path.Combine(block, ".umpireconfig"), "[umpire.check.folder_aliases]\n\"Assets/Shop\" = [\"\"]\n");

        Assert.That(DefinitionFolder.Below(block, []), Is.Empty);
    }

    [Test]
    public void A_folder_with_its_own_config_file_and_project_is_another_root() {
        Write("Tools/.umpireconfig");
        Write("Tools/Tools.csproj");
        Write("Tools/Definition/ToolsArchitecture.cs");

        Assert.That(DefinitionFolder.Below(block, []), Is.Empty);
    }

    void Write(string path) {
        var file = Path.Combine(block, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "");
    }
}
