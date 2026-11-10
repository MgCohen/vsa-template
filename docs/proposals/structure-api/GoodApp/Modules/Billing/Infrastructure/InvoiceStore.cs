using System.Collections.Concurrent;
using GoodApp.Modules.Billing.Domain;
using GoodApp.Modules.Billing.Domain.Shared;

namespace GoodApp.Modules.Billing.Infrastructure;

public sealed class InvoiceStore
{
    internal ConcurrentDictionary<InvoiceId, Invoice> Invoices { get; } = new();
}
