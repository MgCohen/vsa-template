namespace Umpire.Vsa.Views;

using Microsoft.AspNetCore.Components;
using Umpire.Vsa.Presenters;

public abstract class PresenterComponent<T> : PresenterWatcher where T : Presenter {
    [Parameter, EditorRequired] public T Presenter { get; set; } = default!;

    protected sealed override void OnParametersSet() => Watch(Presenter);
}
