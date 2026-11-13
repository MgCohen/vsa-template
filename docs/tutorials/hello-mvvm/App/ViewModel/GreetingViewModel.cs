using Hello.App.Model;
using Hello.Engine.ViewModel;

namespace Hello.App.ViewModel;

public sealed class GreetingViewModel(Greeting greeting) : ViewModelBase<Greeting>(greeting) {
    public string Text => $"Hello {Model.Name}";
}
