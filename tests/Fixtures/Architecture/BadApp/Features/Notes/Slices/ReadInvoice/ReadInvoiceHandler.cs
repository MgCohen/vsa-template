using BadApp.Billing.Domain;

namespace BadApp.Notes.Slices.ReadInvoice;

public sealed class ReadInvoiceHandler : Kit.IRequestHandler {
    public static Invoice Handle(ReadInvoiceRequest request) {
        return new Invoice(request.Id, 0);
    }
}
