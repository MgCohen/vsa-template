using Microsoft.AspNetCore.Components;
using VSA.Presenters;

namespace VSA.Views;

public abstract class PresenterComponent<T> : ComponentBase, IDisposable where T : Presenter {
    T? subscribed;

    [Parameter, EditorRequired] public T Presenter { get; set; } = default!;

    // Subscribes to Presenter.Changed and re-renders with InvokeAsync(StateHasChanged).
    protected sealed override void OnParametersSet() {
        if (ReferenceEquals(subscribed, Presenter)) {
            return;
        }
        if (subscribed is not null) {
            subscribed.Changed -= OnChanged;
        }
        subscribed = Presenter;
        subscribed.Changed += OnChanged;
    }

    public void Dispose() {
        if (subscribed is not null) {
            subscribed.Changed -= OnChanged;
        }
        GC.SuppressFinalize(this);
    }

    void OnChanged() => _ = InvokeAsync(StateHasChanged);
}
