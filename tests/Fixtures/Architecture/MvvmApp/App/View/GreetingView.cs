using Mvvm.App.ViewModel;
using Mvvm.Engine.View;

namespace Mvvm.App.View;

public sealed class GreetingView(GreetingViewModel viewModel) : ViewBase<GreetingViewModel>(viewModel) {
    public override string Render() => new Mvvm.App.Model.Greeting().Name;
}
