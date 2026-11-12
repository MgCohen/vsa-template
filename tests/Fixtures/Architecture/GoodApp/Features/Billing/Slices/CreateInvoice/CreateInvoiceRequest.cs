namespace GoodApp.Billing.Slices.CreateInvoice;

public sealed record CreateInvoiceRequest(decimal Amount) : Kit.IRequest;
