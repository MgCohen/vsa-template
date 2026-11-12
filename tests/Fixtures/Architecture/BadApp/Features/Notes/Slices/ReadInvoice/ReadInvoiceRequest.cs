namespace BadApp.Notes.Slices.ReadInvoice;

public sealed record ReadInvoiceRequest(Guid Id) : Kit.IRequest;
