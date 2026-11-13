using Hello.App.Model;
using Hello.App.View;
using Hello.App.ViewModel;
using NUnit.Framework;

namespace Hello.Tests;

public class GreetingTests {
    [Test]
    public void The_view_says_hello_world() {
        var view = new GreetingView(new GreetingViewModel(new Greeting()));

        Assert.That(view.Render(), Is.EqualTo("Hello world"));
    }
}
