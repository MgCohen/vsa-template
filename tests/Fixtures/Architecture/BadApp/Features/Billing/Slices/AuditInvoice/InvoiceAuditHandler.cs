namespace BadApp.Billing.Slices.AuditInvoice;

using BadApp.Billing.Slices.CreateInvoice;

public sealed class InvoiceAuditHandler : Kit.IEventHandler {
    public static string Handle(InvoiceCreated created) {
        return $"Audit {created.Id}";
    }
}
