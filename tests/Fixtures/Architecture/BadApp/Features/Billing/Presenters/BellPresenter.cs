using BadApp.Billing.Domain;
using BadApp.Billing.Slices.CreateInvoice;
using Kit;

namespace BadApp.Billing.Presenters;

public sealed class BellPresenter : IPresenterEvent<InvoiceCreated>, IBellSource, IEquatable<Invoice> {
    internal InvoiceCreated? Last { get; private set; }

    public InvoiceCreated? Current => Last;

    public InvoiceCreated? Latest => Last;

    public Task OnEventAsync(InvoiceCreated e) {
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
