using Microsoft.AspNetCore.Components;
using VSA.Presenters;

namespace VSA.Views;

// A component that shows one presenter: the page gives it as the Presenter parameter.
// For a component that shows more than one presenter, inherit PresenterWatcher and call Watch.
public abstract class PresenterComponent<T> : PresenterWatcher where T : Presenter {
    [Parameter, EditorRequired] public T Presenter { get; set; } = default!;

    protected sealed override void OnParametersSet() => Watch(Presenter);
}
