namespace BadApp.Notes.Slices.ReadInvoice;

using BadApp.Billing.Domain;

public sealed class ReadInvoiceHandler : Kit.IRequestHandler {
    public static Invoice Handle(ReadInvoiceRequest request) {
        return new Invoice(request.Id, 0);
    }
}
