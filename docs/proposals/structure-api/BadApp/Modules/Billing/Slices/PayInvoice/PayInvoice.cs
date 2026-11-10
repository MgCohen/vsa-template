using BadApp.Modules.Billing.Domain;
using BadApp.Modules.Billing.Domain.Shared;
using BadApp.Modules.Billing.Infrastructure;

namespace BadApp.Modules.Billing.Slices.PayInvoice;

public record PayInvoiceRequest(InvoiceId Id) : Fw.IRequest;

public class PayInvoiceHandler(InvoiceStore store) : Fw.Handler<PayInvoiceRequest>
{
    public override void Handle(PayInvoiceRequest request) => store.Save(new Invoice(request.Id));
}
