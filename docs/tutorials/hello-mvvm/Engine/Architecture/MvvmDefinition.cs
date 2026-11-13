using Hello.Engine.Model;
using Hello.Engine.View;
using Hello.Engine.ViewModel;
using VSA.Architecture;

namespace Hello.Engine.Architecture;

public sealed class MvvmDefinition : ArchitectureDefinition {
    protected override void Configure(Options options) {
        options.DefaultPermission = Permission.Deny;
        options.UseSameLayer = true;
    }

    protected override void Define(Root root) {
        root.Add<Engine>().Add<Model>().And<ViewModel>().And<View>();
        var app = root.Add<App>();
        app.Add<Model>().OfType<ModelBase>();
        app.Add<ViewModel>().OfType(typeof(ViewModelBase<>));
        app.Add<View>().OfType(typeof(ViewBase<>));
    }

    protected override void Rules(RuleBook rules) {
        rules.For<View>().Uses<ViewModel>();
        rules.For<ViewModel>().Uses<Model>();
        rules.For<Engine>(Depth.Self).Uses<Engine>();
        rules.For<App>(Depth.Self).Uses<Engine>().And<View>().And<ViewModel>().And<Model>();
    }
}
