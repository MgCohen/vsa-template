using Mvvm.Engine.Model;

namespace Mvvm.Engine.ViewModel;

public abstract class ViewModelBase<TModel>(TModel model) where TModel : ModelBase {
    protected TModel Model { get; } = model;
}
