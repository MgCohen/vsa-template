using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using VSA.Presenters;

namespace VSA.Views;

public abstract class PresenterComponent<T> : ComponentBase, IDisposable where T : Presenter {
    T? subscribed;
    IDisposable? live;

    [Parameter, EditorRequired] public T Presenter { get; set; } = default!;

    [Inject] IServiceProvider Services { get; set; } = default!;

    // Subscribes to Presenter.Changed and re-renders with InvokeAsync(StateHasChanged).
    // Adds the presenter to LivePresenters (if the host registers it), so the presenter gets its presenter events
    // on the dispatcher of this view.
    protected sealed override void OnParametersSet() {
        if (ReferenceEquals(subscribed, Presenter)) {
            return;
        }
        Release();
        subscribed = Presenter;
        subscribed.Changed += OnChanged;
        live = Services.GetService<LivePresenters>()?.Add(subscribed, work => InvokeAsync(work));
    }

    public void Dispose() {
        Release();
        GC.SuppressFinalize(this);
    }

    void Release() {
        live?.Dispose();
        live = null;
        if (subscribed is not null) {
            subscribed.Changed -= OnChanged;
        }
    }

    void OnChanged() => _ = InvokeAsync(StateHasChanged);
}
