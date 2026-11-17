namespace OneLine.Tests;

using OneLine.App.Host;

public sealed class AppTests {
    [Test]
    public void The_app_has_a_name() => Assert.That(OneLineApp.Name, Is.Not.Empty);
}
