namespace Umpire.Vsa.Views;

using Microsoft.AspNetCore.Components;
using Umpire.Vsa.Presenters;

public abstract class PresenterWatcher : ComponentBase, IDisposable {
    Presenter[] watched = [];
    bool disposed;

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
