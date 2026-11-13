using Hello.App.ViewModel;
using Hello.Engine.View;

namespace Hello.App.View;

public sealed class GreetingView(GreetingViewModel viewModel) : ViewBase<GreetingViewModel>(viewModel) {
    public override string Render() => ViewModel.Text;
}
