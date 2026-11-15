using Microsoft.AspNetCore.Components;
using VSA.Presenters;

namespace VSA.Views;

// The base of a component that shows one or more presenters. It only renders: it listens to Changed and calls
// StateHasChanged on the thread of the component. The presenters get their events by themselves, with or without a view.
// A component that combines modules calls Watch with the presenter of each module, in OnParametersSet:
//   protected override void OnParametersSet() => Watch(Hand, Pool);
public abstract class PresenterWatcher : ComponentBase, IDisposable {
    Presenter[] watched = [];
    bool disposed;

    // Listens to these presenters, and stops to listen to the others. The same presenters again change nothing.
    protected void Watch(params Presenter[] presenters) {
        if (watched.SequenceEqual(presenters, ReferenceEqualityComparer.Instance)) {
            return;
        }
        Unwatch();
        watched = [.. presenters];
        foreach (var presenter in watched) {
            presenter.Changed += OnChanged;
        }
    }

    // Changed can come on any thread. InvokeAsync renders on the thread of the component.
    async void OnChanged() {
        if (disposed) {
            return;
        }
        try {
            await InvokeAsync(StateHasChanged);
        } catch (Exception e) {
            await DispatchExceptionAsync(e);
        }
    }

    void Unwatch() {
        foreach (var presenter in watched) {
            presenter.Changed -= OnChanged;
        }
        watched = [];
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            disposed = true;
            Unwatch();
        }
    }
}
