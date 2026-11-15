namespace GoodApp.Billing.Presenters;

using GoodApp.Billing.Domain;
using GoodApp.Billing.Slices.CreateInvoice;
using Kit;

public sealed class BellPresenter : IPresenterEvent<InvoiceCreated>, IPresenterEvent<Invoice> {
    internal InvoiceCreated? Last { get; private set; }

    public Task OnEventAsync(InvoiceCreated e) {
        Last = e;
        return Task.CompletedTask;
    }

    Task IPresenterEvent<Invoice>.OnEventAsync(Invoice e) {
        return Task.CompletedTask;
    }
}
