namespace BadApp.Notes.Slices.ArchiveInvoice;

using BadApp.Billing.Domain;

public sealed class ArchiveInvoiceHandler : Kit.IRequestHandler {
    public static async Task<string> InAsyncMethod(ArchiveInvoiceRequest request) {
        await Task.Yield();
        return new Invoice(request.Id, 0).ToString();
    }

    public static decimal InLambda(ArchiveInvoiceRequest request) {
        return new[] { request.Id }.Select(id => new Invoice(id, 0).Amount).Sum();
    }

    public static decimal InClosure(ArchiveInvoiceRequest request) {
        return new[] { 1m }.Select(amount => new Invoice(request.Id, amount).Amount).Sum();
    }

    public static decimal InLocalFunction(ArchiveInvoiceRequest request) {
        return Amount();

        decimal Amount() => new Invoice(request.Id, 0).Amount;
    }

    public static IEnumerable<decimal> InIterator(ArchiveInvoiceRequest request) {
        yield return new Invoice(request.Id, 0).Amount;
    }
}
