namespace Umpire.Vsa.Views;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Vsa.Presenters;

public abstract class PresenterWatcher : ComponentBase, IDisposable {
    Presenter[] watched = [];
    bool disposed;

    [Inject] IServiceProvider Services { get; set; } = default!;

    protected void Watch(params Presenter[] presenters) {
        MakeTheScopeLive();
        if (Enumerable.SequenceEqual(watched, presenters, ReferenceEqualityComparer.Instance)) {
            return;
        }
        Unwatch();
        watched = [.. presenters];
        foreach (var presenter in watched) {
            presenter.Changed += OnChanged;
        }
    }

    void MakeTheScopeLive() => Services.GetService<PresenterScope>();

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
