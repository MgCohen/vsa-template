namespace BadApp.Notes.Slices.ArchiveInvoice;

public sealed record ArchiveInvoiceRequest(Guid Id) : Kit.IRequest;
