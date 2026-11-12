using BadApp.Billing.Domain;
using BadApp.Billing.Presenters;

namespace BadApp.Billing.Slices.CreateInvoice;

public sealed class CreateInvoiceHandler(InvoicePresenter presenter) : Kit.IRequestHandler {
    public (Invoice, InvoiceCreated) Handle(CreateInvoiceRequest request) {
        var invoice = new Invoice(Guid.NewGuid(), request.Amount);
        presenter.Shown = invoice;
        return (invoice, new InvoiceCreated(invoice.Id));
    }
}
