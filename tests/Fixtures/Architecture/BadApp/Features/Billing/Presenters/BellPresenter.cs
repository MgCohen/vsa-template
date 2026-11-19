namespace BadApp.Billing.Presenters;

using BadApp.Billing.Domain;
using BadApp.Billing.Slices.CreateInvoice;
using Kit;

public sealed class BellPresenter : IPresenterHandler<InvoiceCreated>, IBellSource, IEquatable<Invoice> {
    internal InvoiceCreated? Last { get; private set; }

    public InvoiceCreated? Current => Last;

    public InvoiceCreated? Latest => Last;

    public Task HandleAsync(InvoiceCreated e) {
        Last = e;
        return Task.CompletedTask;
    }

    public void Replay(InvoiceCreated e) {
        Last = e;
    }

    public bool Equals(Invoice? other) {
        return other is not null;
    }
}
