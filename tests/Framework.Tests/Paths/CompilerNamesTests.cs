namespace Framework.Tests.Paths;

using Umpire.Utility;

public sealed class CompilerNamesTests {
    [TestCase("<>c", true)]
    [TestCase("<Main>$", true)]
    [TestCase("Invoice", false)]
    public void A_type_name_that_starts_with_an_angle_bracket_is_compiler_generated(string name, bool generated) {
        Assert.That(CompilerNames.IsGenerated(name), Is.EqualTo(generated));
    }
}
