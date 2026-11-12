namespace BadApp.Billing.Slices.CreateInvoice;

public sealed record InvoiceCreated(Guid Id) : Kit.IEvent;
