namespace GoodApp.Billing.Presenters;

using GoodApp.Billing.Domain;
using GoodApp.Billing.Slices.CreateInvoice;
using Kit;

public sealed class BellPresenter : IPresenterHandler<InvoiceCreated>, IPresenterHandler<Invoice> {
    internal InvoiceCreated? Last { get; private set; }

    public Task HandleAsync(InvoiceCreated e) {
        Last = e;
        return Task.CompletedTask;
    }

    Task IPresenterHandler<Invoice>.HandleAsync(Invoice e) {
        return Task.CompletedTask;
    }
}
