namespace VSA.Presenters;

public abstract class Presenter(IBus bus) {
    public event Action? Changed;

    protected IBus Bus => bus;

    // Sets Busy and Error on op, catches RequestException, raises Changed.
    protected async Task Run(OpState op, Func<Task> body) {
        op.Busy = true;
        op.Error = null;
        Changed?.Invoke();
        try {
            await body();
        } catch (RequestException e) {
            op.Error = e.Message;
        } finally {
            op.Busy = false;
            Changed?.Invoke();
        }
    }
}
