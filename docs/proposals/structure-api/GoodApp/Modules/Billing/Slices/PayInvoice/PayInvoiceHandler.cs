using GoodApp.Modules.Billing.Domain;
using GoodApp.Modules.Billing.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace GoodApp.Modules.Billing.Slices.PayInvoice;

sealed class PayInvoiceHandler(IRepository<Invoice, InvoiceId> invoices) : RequestHandler<PayInvoiceRequest, PayInvoiceResponse>
{
    static readonly Error NotFound = new("billing.invoice_not_found", "The invoice does not exist.");

    public override async Task<Result<PayInvoiceResponse>> HandleAsync(Envelope<PayInvoiceRequest> envelope)
    {
        var invoice = await invoices.GetAsync(new InvoiceId(envelope.Message.InvoiceId), envelope.Cancellation);
        if (invoice is null) return NotFound;
        invoice.Pay();
        await invoices.SaveChangesAsync(envelope.Cancellation);
        return new PayInvoiceResponse(invoice.Paid);
    }
}
