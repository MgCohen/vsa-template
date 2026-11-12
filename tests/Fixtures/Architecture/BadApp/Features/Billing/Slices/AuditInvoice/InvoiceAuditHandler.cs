using BadApp.Billing.Slices.CreateInvoice;

namespace BadApp.Billing.Slices.AuditInvoice;

public sealed class InvoiceAuditHandler : Kit.IEventHandler {
    public static string Handle(InvoiceCreated created) {
        return $"Audit {created.Id}";
    }
}
