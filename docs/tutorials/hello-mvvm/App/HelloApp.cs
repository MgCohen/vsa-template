using Hello.App.Model;
using Hello.App.View;
using Hello.App.ViewModel;
using Hello.Engine.Architecture;
using VSA.Architecture;
using VSA.Composition;

namespace Hello.App;

public sealed class HelloApp : VsaApp<HelloApp> {
    public override ArchitectureDefinition Architecture => new MvvmDefinition();

    protected override int Start(string[] args) {
        var view = new GreetingView(new GreetingViewModel(new Greeting()));
        Console.WriteLine(view.Render());
        return base.Start(args);
    }
}
