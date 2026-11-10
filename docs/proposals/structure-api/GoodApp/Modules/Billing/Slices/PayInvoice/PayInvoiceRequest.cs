using VSA.Contracts.Requests;

namespace GoodApp.Modules.Billing.Slices.PayInvoice;

public sealed record PayInvoiceRequest(Guid InvoiceId) : IRequest<PayInvoiceResponse>
{
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/invoices/{InvoiceId}/pay";
}
