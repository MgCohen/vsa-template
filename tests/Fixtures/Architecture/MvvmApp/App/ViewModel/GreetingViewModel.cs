using Mvvm.App.Model;
using Mvvm.Engine.ViewModel;

namespace Mvvm.App.ViewModel;

public sealed class GreetingViewModel(Greeting greeting) : ViewModelBase<Greeting>(greeting) {
    public string Text => $"Hello {Model.Name}";
}
