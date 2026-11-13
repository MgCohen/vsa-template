namespace Hello.Engine.View;

public abstract class ViewBase<TViewModel>(TViewModel viewModel) where TViewModel : class {
    protected TViewModel ViewModel { get; } = viewModel;

    public abstract string Render();
}
